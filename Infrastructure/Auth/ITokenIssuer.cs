namespace Bookkeeping.Infrastructure.Auth;

// Builds a signed JWT for an authenticated principal. Kept in Infrastructure because
// token formats are a delivery/security concern, not a domain one.
public interface ITokenIssuer
{
    // userId is the domain UserId value; it becomes the token's `sub` claim so
    // endpoints can resolve the domain user (and ownership) straight from the token.
    string Issue(Guid userId, string email);
}
