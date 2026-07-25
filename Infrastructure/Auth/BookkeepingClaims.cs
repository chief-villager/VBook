namespace Bookkeeping.Infrastructure.Auth;

// Custom claim types carried in the JWT beyond the standard registered claims.
public static class BookkeepingClaims
{
    // One per business the user belongs to. Value is "{businessId}:{role}" so a
    // single token can carry a distinct role for each business (roles are
    // per-business). An authorization handler parses the business id off the route
    // and matches it against these claims to decide access.
    public const string BusinessRole = "business_role";

    // A capability granted to a role, seeded onto Identity roles as role claims and
    // (once enforcement is wired) expanded into a user's token so policies can check it.
    public const string Permission = "Permission";
}
