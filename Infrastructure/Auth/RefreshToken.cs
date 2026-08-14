namespace Bookkeeping.Infrastructure.Auth;

// A persisted refresh token. This is an authentication concern, not a domain
// aggregate, so it lives in Infrastructure alongside ApplicationUser and is stored
// in the `auth` schema. Only the SHA-256 *hash* of the token is kept — the raw
// value is handed to the client once and never stored, so a database leak can't
// reveal a usable token.
//
// Tokens are single-use and rotated: each refresh revokes the presented token and
// issues a new one in the same FamilyId. A family is one login session's lineage;
// revoking the family logs that session out everywhere and is also how reuse of an
// already-rotated token (a theft signal) is neutralised.
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    // The login session this token descends from. All tokens minted by rotating the
    // original share it, so revoking by family kills the whole chain at once.
    public Guid FamilyId { get; private set; }

    // Base64 SHA-256 of the raw token. Lookups hash the presented value and match here.
    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Set when the token is consumed (rotated) or revoked (logout / reuse detection).
    public DateTime? RevokedAt { get; private set; }

    // The token that superseded this one when it was rotated. Null until rotated.
    public Guid? ReplacedById { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, Guid familyId, string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTime nowUtc) => RevokedAt is null && ExpiresAt > nowUtc;

    public void Revoke(DateTime nowUtc, Guid? replacedById = null)
    {
        // Idempotent: keep the first revocation timestamp so audit order is preserved.
        RevokedAt ??= nowUtc;
        ReplacedById ??= replacedById;
    }
}
