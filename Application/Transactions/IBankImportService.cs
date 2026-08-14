using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

// The bank-import review queue: pull a linked account's feed into a quarantine of
// pending rows, let the user categorise/approve/discard, and promote approved rows
// into real Transactions (which the Ledger turns into balanced journal entries).
public interface IBankImportService
{
    Task<Result<BankImportSummary>> PullAsync(PullBankFeedCommand command, CancellationToken ct = default);
    Task<PagedResult<StagedTransactionDto>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, PageRequest page, CancellationToken ct = default);
    // businessId scopes the resource: the staged row must belong to it, else "not found".
    Task<Result> CategoriseAsync(BusinessId businessId, StagedTransactionId id, CategoryId categoryId, CancellationToken ct = default);
    Task<Result<TransactionId>> ApproveAsync(BusinessId businessId, StagedTransactionId id, CancellationToken ct = default);
    Task<Result> DiscardAsync(BusinessId businessId, StagedTransactionId id, CancellationToken ct = default);
}
