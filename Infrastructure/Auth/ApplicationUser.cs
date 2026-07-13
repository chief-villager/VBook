using Microsoft.AspNetCore.Identity;

namespace Bookkeeping.Infrastructure.Auth;

// The authentication principal — password hash, lockout, 2FA and tokens all come
// from IdentityUser. This is NOT the domain User: the accounting model stays pure.
// Its Id is the same Guid as the domain User.Id.Value, and that shared value is the
// only link between them (no foreign key), exactly like Business.OwnerId references
// a user across a module boundary.
public sealed class ApplicationUser : IdentityUser<Guid>
{
}

 