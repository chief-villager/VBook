using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Transactions;

public interface ITransactionService
{
    Task<Result<TransactionId>> RecordAsync(RecordTransactionCommand command, CancellationToken ct = default);
    Task<Result> VoidAsync(TransactionId id, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionSummary>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(BusinessId businessId, CancellationToken ct = default);
}
