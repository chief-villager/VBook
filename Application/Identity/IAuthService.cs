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

    // Verifies email/password and returns a short-lived access token plus a refresh
    // token on success.
    Task<Result<AuthTokens>> SignInAsync(string email, string password, CancellationToken ct = default);

    // Exchanges a valid refresh token for a new token pair, rotating the refresh token
    // (the presented one is revoked). Fails if the token is unknown, expired, or has
    // already been rotated (reuse — which revokes the whole session).
    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default);

    // Logs the session out by revoking the refresh token's family. Idempotent: an
    // unknown or stale token still succeeds (nothing to revoke).
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> ConfirmEmailAsync(string email, string token, CancellationToken ct = default);
    Task<Result<string>> GetEmailConfirmationTokenAsync(string email, CancellationToken ct = default);

    // Generates a fresh confirmation link for the user and emails it. Best-effort:
    // a delivery failure is reported via Result but never throws, so it can't roll
    // back a committed registration. Call after the account is persisted.
    Task<Result> SendEmailConfirmationAsync(string email, CancellationToken ct = default);
}
