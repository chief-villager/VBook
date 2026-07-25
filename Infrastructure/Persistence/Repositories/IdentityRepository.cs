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

    // The invoice template belongs to the Business aggregate (shared PK on BusinessId);
    // a business may not have one yet, so this can return null.
    public Task<InvoiceTemplate?> GetBusinessInvoiceTemplateAsync(BusinessId id, CancellationToken ct = default)
        => Context.Set<InvoiceTemplate>().FirstOrDefaultAsync(t => t.BusinessId == id, ct);

    public Task<User?> GetUserAsync(UserId id, CancellationToken ct = default)
        => Context.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        => Context.Set<User>().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default)
        => await Context.Set<User>().AddAsync(user, ct);

    public async Task AddMembershipAsync(BusinessMembership membership, CancellationToken ct = default)
        => await Context.Set<BusinessMembership>().AddAsync(membership, ct);

    public Task<BusinessMembership?> GetMembershipAsync(BusinessId businessId, UserId userId, CancellationToken ct = default)
        => Context.Set<BusinessMembership>()
            .FirstOrDefaultAsync(m => m.BusinessId == businessId && m.UserId == userId, ct);

    public async Task<IReadOnlyList<BusinessMembership>> ListMembershipsAsync(BusinessId businessId, CancellationToken ct = default)
        => await Context.Set<BusinessMembership>()
            .Where(m => m.BusinessId == businessId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BusinessMembership>> ListMembershipsForUserAsync(UserId userId, CancellationToken ct = default)
        => await Context.Set<BusinessMembership>()
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);
}
