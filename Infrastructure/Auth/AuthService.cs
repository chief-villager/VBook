using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenIssuer _tokens;
    private readonly IIdentityRepository _identity;
    private readonly IOptions<Hosting> _hosting;

    public AuthService(
    UserManager<ApplicationUser> users, 
    ITokenIssuer tokens, 
    IIdentityRepository identity, 
    IOptions<Hosting> hosting)
    {
        _users = users;
        _tokens = tokens;
        _identity = identity;
        _hosting = hosting;
    }

    public async Task<Result<bool>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<bool>.Failure("Invalid credentials.");

        var result = await _users.ResetPasswordAsync(principal, token, newPassword);
        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> CreateCredentialsAsync(UserId userId, string email, string password, CancellationToken ct = default)
    {
        // Id == the domain user's id: this shared Guid is the link (no FK).
        // Email is confirmed on creation: there's no email sender wired to deliver a
        // confirmation link, and the sign-in gate requires a confirmed email — so
        // without this, no registered user could ever log in.
        var principal = new ApplicationUser { Id = userId.Value, UserName = email, Email = email, EmailConfirmed = true };

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
        var callbackurl = $"{_hosting.Value.Urls}/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return Result<string>.Success(callbackurl);
    }
    public async Task<Result<bool>> ConfirmEmailAsync(string email, string token, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null)
            return Result<bool>.Failure("Invalid credentials.");

        var result = await _users.ConfirmEmailAsync(principal, token);
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
        var callbackurl = $"{_hosting.Value.Urls}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return Result<string>.Success(callbackurl);
    }

 

    public async Task<Result<string>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null || !await _users.CheckPasswordAsync(principal, password))
            return Result<string>.Failure("Invalid credentials.");

        // Require a confirmed email before issuing a token. No emailer is wired, so the
        // confirmation link is reissued and surfaced in the failure (same dev convention
        // as the password-reset / email-confirmation endpoints) rather than sent out of band.
        if (!await _users.IsEmailConfirmedAsync(principal))
        {
            var confirmation = await GetEmailConfirmationTokenAsync(email, ct);
            return confirmation.IsSuccess
                ? Result<string>.Failure($"Confirm your email before signing in: {confirmation.Value}")
                : Result<string>.Failure("Confirm your email before signing in.");
        }

        // Stamp each business role the user holds into the token so authorization can
        // scope decisions per business (the shared Guid links principal to domain user).
        var memberships = await _identity.ListMembershipsForUserAsync(new UserId(principal.Id), ct);
        var roles = memberships
            .Select(m => new BusinessRoleAssignment(m.BusinessId.Value, m.Role.ToString()))
            .ToList();

        return _tokens.Issue(principal.Id, principal.Email!, roles);
    }
}
