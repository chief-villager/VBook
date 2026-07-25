using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public interface ILinkedBankAccountRepository : IGenericRepository<LinkedBankAccount>
{
    Task<LinkedBankAccount?> GetAsync(LinkedBankAccountId id, CancellationToken ct = default);
    Task<IReadOnlyList<LinkedBankAccount>> ListAsync(
        BusinessId businessId, LinkedBankAccountStatus status, CancellationToken ct = default);

    // Idempotent linking: is this account already actively linked to the business?
    Task<LinkedBankAccount?> GetActiveByExternalIdAsync(
        BusinessId businessId, string externalAccountId, CancellationToken ct = default);
}
