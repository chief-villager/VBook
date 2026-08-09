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
    // categoryId is optional: pass it to categorise-and-approve in one step (the review
    // UI's dropdown + Approve button), or omit it to approve a row already categorised
    // via CategoriseAsync.
    Task<Result<TransactionId>> ApproveAsync(
        StagedTransactionId id, CategoryId? categoryId = null, CancellationToken ct = default);
    Task<Result> DiscardAsync(StagedTransactionId id, CancellationToken ct = default);
}
