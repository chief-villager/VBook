using System.Security.Cryptography;
using Bookkeeping.Domain.Common;
using Bookkeeping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bookkeeping.Infrastructure.Auth;

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    // 256 bits of entropy: unguessable, so the token itself is the only proof of
    // possession (we never store the raw value, only its hash).
    private const int TokenBytes = 32;
    private const int DefaultLifetimeDays = 14;

    private readonly AppDbContext _db;
    private readonly int _lifetimeDays;

    public RefreshTokenStore(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _lifetimeDays = config.GetValue<int?>("Jwt:RefreshTokenDays") ?? DefaultLifetimeDays;
    }

    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        // A brand-new session gets its own family id.
        var (_, issued) = await CreateAsync(userId, Guid.NewGuid(), ct);
        await _db.SaveChangesAsync(ct);
        return issued;
    }

    public async Task<Result<RefreshRotation>> RotateAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Result<RefreshRotation>.Failure("Invalid refresh token.");

        var now = DateTime.UtcNow;
        var hash = Hash(rawToken);
        var token = await _db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
            return Result<RefreshRotation>.Failure("Invalid refresh token.");

        // Reuse detection: a token that has already been rotated (or revoked) is being
        // presented again. The legitimate holder rotated it away, so a second presenter
        // is likely an attacker — burn the whole family and force a fresh login.
        if (!token.IsActive(now))
        {
            await RevokeFamilyAsync(token.FamilyId, now, ct);
            await _db.SaveChangesAsync(ct);
            return Result<RefreshRotation>.Failure("Invalid refresh token.");
        }

        // Rotate: mint the successor in the same family and link the old token to it.
        var (successor, issued) = await CreateAsync(token.UserId, token.FamilyId, ct);
        token.Revoke(now, successor.Id);

        await _db.SaveChangesAsync(ct);
        return Result<RefreshRotation>.Success(new RefreshRotation(token.UserId, issued.RawToken, issued.ExpiresAt));
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return;

        var hash = Hash(rawToken);
        var token = await _db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null)
            return;

        await RevokeFamilyAsync(token.FamilyId, DateTime.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
    }

    // Stages (does not save) a new token record and returns the entity alongside the
    // raw value + expiry to hand to the client.
    private async Task<(RefreshToken Entity, IssuedRefreshToken Issued)> CreateAsync(
        Guid userId, Guid familyId, CancellationToken ct)
    {
        var raw = Base64Url(RandomNumberGenerator.GetBytes(TokenBytes));
        var expiresAt = DateTime.UtcNow.AddDays(_lifetimeDays);
        var entity = new RefreshToken(userId, familyId, Hash(raw), expiresAt);
        await _db.Set<RefreshToken>().AddAsync(entity, ct);
        return (entity, new IssuedRefreshToken(raw, expiresAt));
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken ct)
    {
        var family = await _db.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in family)
            t.Revoke(now);
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
