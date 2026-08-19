using Bookkeeping.Application.Common;
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

    public async Task<PagedResult<Transaction>> ListPagedAsync(
        BusinessId businessId, DateRange period, PageRequest page, CancellationToken ct = default)
    {
        var query = Set.Where(t =>
            t.BusinessId == businessId && t.OccurredOn >= period.Start && t.OccurredOn <= period.End);

        var total = await query.CountAsync(ct);

        // Newest first; Id is the tiebreaker so paging is deterministic when several
        // transactions share an OccurredOn date.
        var items = await query
            .OrderByDescending(t => t.OccurredOn)
            .ThenByDescending(t => t.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Transaction>(items, page.Page, page.PageSize, total);
    }

    public Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default)
        => Context.Set<Category>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default)
        => await Context.Set<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Transaction?> GetEarliestTransactionDateAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var earliestTransaction = await Set
            .Where(t => t.BusinessId == businessId)
            .OrderBy(t => t.OccurredOn)
            .Select(t => t)
            .FirstOrDefaultAsync(ct);
        return earliestTransaction;
    }
}
