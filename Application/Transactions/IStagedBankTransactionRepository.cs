using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public interface IStagedBankTransactionRepository : IGenericRepository<StagedBankTransaction>
{
    Task<StagedBankTransaction?> GetAsync(StagedTransactionId id, CancellationToken ct = default);
    Task<IReadOnlyList<StagedBankTransaction>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, CancellationToken ct = default);

    // Dedupe: which of these feed ids are already staged for this business?
    Task<HashSet<string>> ExistingExternalIdsAsync(
        BusinessId businessId, IEnumerable<string> externalIds, CancellationToken ct = default);
}
