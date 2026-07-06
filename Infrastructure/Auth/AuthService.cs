using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenIssuer _tokens;

    public AuthService(UserManager<ApplicationUser> users, ITokenIssuer tokens)
    {
        _users = users;
        _tokens = tokens;
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

        return _tokens.Issue(principal.Id, principal.Email!);
    }
}
