using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

// The bank-import review queue: pull a linked account's feed into a quarantine of
// pending rows, let the user categorise/approve/discard, and promote approved rows
// into real Transactions (which the Ledger turns into balanced journal entries).
public interface IBankImportService
{
    Task<Result<BankImportSummary>> PullAsync(PullBankFeedCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<StagedTransactionDto>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, CancellationToken ct = default);
    Task<Result> CategoriseAsync(StagedTransactionId id, CategoryId categoryId, CancellationToken ct = default);
    Task<Result<TransactionId>> ApproveAsync(StagedTransactionId id, CancellationToken ct = default);
    Task<Result> DiscardAsync(StagedTransactionId id, CancellationToken ct = default);
}
