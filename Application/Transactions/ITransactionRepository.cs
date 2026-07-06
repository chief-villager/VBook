using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public interface ITransactionRepository : IGenericRepository<Transaction>
{
    Task<Transaction?> GetAsync(TransactionId id, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);

    Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default);
    // Categories are shared reference data, not scoped to a business.
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default);
}
