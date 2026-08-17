using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Common;
using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenIssuer _tokens;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IIdentityRepository _identity;
    private readonly IOptions<Hosting> _hosting;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
    UserManager<ApplicationUser> users,
    ITokenIssuer tokens,
    IRefreshTokenStore refreshTokens,
    IIdentityRepository identity,
    IOptions<Hosting> hosting,
    IEmailSender email,
    ILogger<AuthService> logger,
    IUnitOfWork unitOfWork)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _identity = identity;
        _hosting = hosting;
        _email = email;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<bool>.Failure("Invalid credentials.");

        var result = await _users.ResetPasswordAsync(principal, token, newPassword);
        await _unitOfWork.SaveChangesAsync(ct); // Ensure the password reset is persisted
        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> CreateCredentialsAsync(UserId userId, string email, string password, CancellationToken ct = default)
    {
        // Id == the domain user's id: this shared Guid is the link (no FK). The account
        // starts with an *unconfirmed* email; the confirmation link is sent once the
        // registration transaction has committed (SendEmailConfirmationAsync, called from
        // IdentityService), and SignInAsync denies sign-in until the user confirms.
        var principal = new ApplicationUser { Id = userId.Value, UserName = email, Email = email };

        // With AutoSaveChanges disabled on the store, this validates and hashes the
        // password and *tracks* the principal without hitting the database.
        var result = await _users.CreateAsync(principal, password);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<string>> GetEmailConfirmationTokenAsync(string email, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<string>.Failure("Invalid credentials.");

        var token = await _users.GenerateEmailConfirmationTokenAsync(principal);
        var callbackurl = $"{_hosting.Value.BaseUrl}/api/auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return Result<string>.Success(callbackurl);
    }

    public async Task<Result> SendEmailConfirmationAsync(string email, CancellationToken ct = default)
    {
        var link = await GetEmailConfirmationTokenAsync(email, ct);
        if (link.IsFailure)
            return Result.Failure(link.Error);

        var body = $"""
            <p>Welcome to Vbook.</p>
            <p>Confirm your email address to activate your account:</p>
            <p><a href="{link.Value}">Confirm my email</a></p>
            <p>If you didn't create this account, you can ignore this message.</p>
            """;

        try
        {
            await _email.SendAsync(email, "Confirm your Vbook email", body, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // Best-effort: never fail a committed registration (or a sign-in attempt)
            // because delivery hiccuped. The link is reissued on the next sign-in.
            _logger.LogWarning(ex, "Failed to send confirmation email to {Email}", email);
            return Result.Failure("Could not send the confirmation email.");
        }
    }
    public async Task<Result<bool>> ConfirmEmailAsync(string email, string token, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<bool>.Failure("Invalid credentials.");

        var result = await _users.ConfirmEmailAsync(principal, token); 
        await _unitOfWork.SaveChangesAsync(ct); // Ensure the confirmation is persisted       
        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    public async Task<Result<string>> GetPasswordResetTokenAsync(string email, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<string>.Failure("Invalid credentials.");

        var token = await _users.GeneratePasswordResetTokenAsync(principal);
        var callbackurl = $"{_hosting.Value.BaseUrl}/api/auth/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return Result<string>.Success(callbackurl);
    }

 

    public async Task<Result<AuthTokens>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null || !await _users.CheckPasswordAsync(principal, password))
            return Result<AuthTokens>.Failure("Invalid credentials.");

        // Deny sign-in until the email is confirmed, and re-send the confirmation link
        // so a user who lost the original can still complete it.
        if (!await _users.IsEmailConfirmedAsync(principal))
        {
            await SendEmailConfirmationAsync(email, ct);
            return Result<AuthTokens>.Failure("Please confirm your email before signing in. We've sent you a fresh confirmation link.");
        }

        // A successful password check starts a new session (its own refresh-token family).
        var access = await IssueAccessTokenAsync(principal, ct);
        var refresh = await _refreshTokens.IssueAsync(principal.Id, ct);
        return Result<AuthTokens>.Success(ToTokens(access, refresh.RawToken, refresh.ExpiresAt));
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        // Rotate first: this validates the presented token, revokes it, and mints its
        // successor (or trips reuse detection and kills the family).
        var rotation = await _refreshTokens.RotateAsync(refreshToken, ct);
        if (rotation.IsFailure)
            return Result<AuthTokens>.Failure(rotation.Error);

        var principal = await _users.FindByIdAsync(rotation.Value.UserId.ToString());
        if (principal is null)
            return Result<AuthTokens>.Failure("Invalid refresh token.");

        // Re-read memberships so role changes take effect on the next refresh rather
        // than being frozen at sign-in for the whole refresh-token lifetime.
        var access = await IssueAccessTokenAsync(principal, ct);
        return Result<AuthTokens>.Success(ToTokens(access, rotation.Value.RawToken, rotation.Value.ExpiresAt));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        await _refreshTokens.RevokeAsync(refreshToken, ct);
        return Result.Success();
    }

    // Stamps each business role the user holds into the access token so authorization
    // can scope decisions per business (the shared Guid links principal to domain user).
    private async Task<IssuedAccessToken> IssueAccessTokenAsync(ApplicationUser principal, CancellationToken ct)
    {
        var memberships = await _identity.ListMembershipsForUserAsync(new UserId(principal.Id), ct);
        var roles = memberships
            .Select(m => new BusinessRoleAssignment(m.BusinessId.Value, m.Role.ToString()))
            .ToList();

        return _tokens.Issue(principal.Id, principal.Email!, roles);
    }

    private static AuthTokens ToTokens(IssuedAccessToken access, string refreshToken, DateTime refreshExpiresAt) =>
        new(access.Token, access.ExpiresAt, refreshToken, refreshExpiresAt);
}
