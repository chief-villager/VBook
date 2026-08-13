using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Identity;

// Authentication seam. The implementation lives in Infrastructure (ASP.NET Core
// Identity), so no Identity types leak above this interface.
public interface IAuthService
{
    // Stages credentials for a domain user on the current unit of work. Does NOT
    // save — the caller's SaveChangesAsync commits the user and credentials together.
    Task<Result> CreateCredentialsAsync(UserId userId, string email, string password, CancellationToken ct = default);
    Task<Result<string>> GetPasswordResetTokenAsync(string email, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default);

    // Verifies email/password and returns a signed JWT on success.
    Task<Result<string>> SignInAsync(string email, string password, CancellationToken ct = default);
    Task<Result<bool>> ConfirmEmailAsync(string email, string token, CancellationToken ct = default);
    Task<Result<string>> GetEmailConfirmationTokenAsync(string email, CancellationToken ct = default);

    // Generates a fresh confirmation link for the user and emails it. Best-effort:
    // a delivery failure is reported via Result but never throws, so it can't roll
    // back a committed registration. Call after the account is persisted.
    Task<Result> SendEmailConfirmationAsync(string email, CancellationToken ct = default);
}
