using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Transactions;

// Connects and disconnects a business's bank accounts through the feed provider,
// persisting the durable account handle so imports can be pulled against it later.
public interface IBankAccountService
{
    Task<Result<LinkedBankAccountDto>> LinkAsync(
        BusinessId businessId, string authorisationCode, CancellationToken ct = default);
    Task<IReadOnlyList<LinkedBankAccountDto>> ListAsync(BusinessId businessId, CancellationToken ct = default);
    Task<Result> UnlinkAsync(LinkedBankAccountId id, CancellationToken ct = default);
}
