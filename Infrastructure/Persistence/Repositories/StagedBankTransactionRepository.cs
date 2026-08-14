using Bookkeeping.Application.Common;
using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class StagedBankTransactionRepository
    : GenericRepository<StagedBankTransaction>, IStagedBankTransactionRepository
{
    public StagedBankTransactionRepository(AppDbContext context) : base(context) { }

    public Task<StagedBankTransaction?> GetAsync(StagedTransactionId id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PagedResult<StagedBankTransaction>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, PageRequest page, CancellationToken ct = default)
    {
        var query = Set.Where(s => s.BusinessId == businessId && s.Status == status);

        var total = await query.CountAsync(ct);

        // Newest first; Id is the tiebreaker so paging is deterministic when several
        // rows share an OccurredOn date.
        var items = await query
            .OrderByDescending(s => s.OccurredOn)
            .ThenByDescending(s => s.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResult<StagedBankTransaction>(items, page.Page, page.PageSize, total);
    }

    public async Task<HashSet<string>> ExistingExternalIdsAsync(
        BusinessId businessId, IEnumerable<string> externalIds, CancellationToken ct = default)
    {
        var ids = externalIds.ToList();
        var existing = await Set
            .Where(s => s.BusinessId == businessId && ids.Contains(s.ExternalId))
            .Select(s => s.ExternalId)
            .ToListAsync(ct);

        return existing.ToHashSet();
    }
}
