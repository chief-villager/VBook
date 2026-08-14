namespace Bookkeeping.Infrastructure.Auth;

// A single business-scoped role assignment to carry in the token. Kept as a plain
// value so no domain type leaks into the token layer.
public readonly record struct BusinessRoleAssignment(Guid BusinessId, string Role);

// A signed access token and the moment it expires. The expiry is surfaced so the
// caller can hand it to the client (which uses it to know when to refresh).
public readonly record struct IssuedAccessToken(string Token, DateTime ExpiresAt);

// Builds a signed JWT for an authenticated principal. Kept in Infrastructure because
// token formats are a delivery/security concern, not a domain one.
public interface ITokenIssuer
{
    // userId is the domain UserId value; it becomes the token's `sub` claim so
    // endpoints can resolve the domain user (and ownership) straight from the token.
    // roles carries the user's per-business memberships as claims so authorization
    // can scope decisions to the business on the route.
    IssuedAccessToken Issue(Guid userId, string email, IReadOnlyCollection<BusinessRoleAssignment> roles);
}
