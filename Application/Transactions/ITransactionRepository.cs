using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public interface ITransactionRepository : IGenericRepository<Transaction>
{
    Task<Transaction?> GetAsync(TransactionId id, CancellationToken ct = default);

    // Full set for the period — used where every row is needed (e.g. credit scoring).
    Task<IReadOnlyList<Transaction>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);

    Task<Transaction?> GetEarliestTransactionDateAsync(BusinessId businessId, CancellationToken ct = default);

    // One page of the period's transactions, newest first, with the total count.
    Task<PagedResult<Transaction>> ListPagedAsync(
        BusinessId businessId, DateRange period, PageRequest page, CancellationToken ct = default);

    Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default);
    // Categories are shared reference data, not scoped to a business.
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default);
}
