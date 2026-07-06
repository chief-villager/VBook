using Bookkeeping.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Auth;

// AutoSaveChanges is disabled so UserManager.CreateAsync only validates, hashes and
// *tracks* the ApplicationUser — it never calls SaveChanges itself. That leaves
// IdentityService's IUnitOfWork as the single commit point, so the domain User and
// its credentials are written in one physical transaction (which is also what
// dispatches domain events).
public sealed class BookkeepingUserStore
    : UserStore<ApplicationUser, IdentityRole<Guid>, AppDbContext, Guid>
{
    public BookkeepingUserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }
}
