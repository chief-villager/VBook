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

    public async Task<IReadOnlyList<StagedBankTransaction>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, CancellationToken ct = default)
        => await Set
            .Where(s => s.BusinessId == businessId && s.Status == status)
            .OrderBy(s => s.OccurredOn)
            .ToListAsync(ct);

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
