using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenIssuer _tokens;
    private readonly IIdentityRepository _identity;

    public AuthService(UserManager<ApplicationUser> users, ITokenIssuer tokens, IIdentityRepository identity)
    {
        _users = users;
        _tokens = tokens;
        _identity = identity;
    }

    public async Task<Result> CreateCredentialsAsync(UserId userId, string email, string password, CancellationToken ct = default)
    {
        // Id == the domain user's id: this shared Guid is the link (no FK).
        var principal = new ApplicationUser { Id = userId.Value, UserName = email, Email = email };

        // With AutoSaveChanges disabled on the store, this validates and hashes the
        // password and *tracks* the principal without hitting the database.
        var result = await _users.CreateAsync(principal, password);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<string>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var principal = await _users.FindByEmailAsync(email);
        if (principal is null || !await _users.CheckPasswordAsync(principal, password))
            return Result<string>.Failure("Invalid credentials.");

        // Stamp each business role the user holds into the token so authorization can
        // scope decisions per business (the shared Guid links principal to domain user).
        var memberships = await _identity.ListMembershipsForUserAsync(new UserId(principal.Id), ct);
        var roles = memberships
            .Select(m => new BusinessRoleAssignment(m.BusinessId.Value, m.Role.ToString()))
            .ToList();

        return _tokens.Issue(principal.Id, principal.Email!, roles);
    }
}
