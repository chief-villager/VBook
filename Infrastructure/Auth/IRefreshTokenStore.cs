using Bookkeeping.Domain.Common;

namespace Bookkeeping.Infrastructure.Auth;

// A freshly minted refresh token. RawToken is the opaque secret handed to the
// client exactly once; only its hash is persisted.
public readonly record struct IssuedRefreshToken(string RawToken, DateTime ExpiresAt);

// The outcome of rotating a presented token: the caller (AuthService) uses UserId
// to mint a matching access token and returns RawToken/ExpiresAt to the client.
public readonly record struct RefreshRotation(Guid UserId, string RawToken, DateTime ExpiresAt);

// Persistence + lifecycle for refresh tokens. Kept in Infrastructure because it is
// an authentication mechanism (hashing, rotation, storage), not a domain concept —
// AuthService is the only consumer and it also lives here.
public interface IRefreshTokenStore
{
    // Starts a new session: issues the first token of a fresh family for the user.
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken ct = default);

    // Validates and rotates the presented token. On success the old token is revoked
    // and a new one is issued in the same family. Expected failures (unknown, expired,
    // or reuse of an already-rotated token) come back as Result.Failure; on reuse the
    // whole family is revoked first, since a replayed token signals theft.
    Task<Result<RefreshRotation>> RotateAsync(string rawToken, CancellationToken ct = default);

    // Revokes the family the presented token belongs to (logout). A no-op for an
    // unknown token, so logout is idempotent and safe to call with a stale value.
    Task RevokeAsync(string rawToken, CancellationToken ct = default);
}
