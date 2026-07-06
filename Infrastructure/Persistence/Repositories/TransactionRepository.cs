using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public Task<Transaction?> GetAsync(TransactionId id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Transaction>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
        => await Set
            .Where(t => t.BusinessId == businessId && t.OccurredOn >= period.Start && t.OccurredOn <= period.End)
            .OrderBy(t => t.OccurredOn)
            .ToListAsync(ct);

    public Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default)
        => Context.Set<Category>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default)
        => await Context.Set<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
}
