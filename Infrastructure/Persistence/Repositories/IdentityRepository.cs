using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class IdentityRepository : GenericRepository<Business>, IIdentityRepository
{
    public IdentityRepository(AppDbContext context) : base(context) { }

    public Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<User?> GetUserAsync(UserId id, CancellationToken ct = default)
        => Context.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default)
        => await Context.Set<User>().AddAsync(user, ct);
}
