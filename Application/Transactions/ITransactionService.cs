using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Transactions;

public interface ITransactionService
{
    Task<Result<TransactionId>> RecordAsync(RecordTransactionCommand command, CancellationToken ct = default);
    Task<Result> VoidAsync(TransactionId id, string reason, CancellationToken ct = default);

    // Full set for the period (e.g. credit scoring).
    Task<IReadOnlyList<TransactionSummary>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);

    // One page of the period's transactions, newest first, with paging metadata.
    Task<PagedResult<TransactionSummary>> ListPagedAsync(
        BusinessId businessId, DateRange period, PageRequest page, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(BusinessId businessId, CancellationToken ct = default);
}
