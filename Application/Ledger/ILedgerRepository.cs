using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;

namespace Bookkeeping.Application.Ledger;

public interface ILedgerRepository : IGenericRepository<JournalEntry>
{
    // The chart of accounts is shared reference data, so these are not scoped to a business.
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default);
    Task<Account?> GetAccountByCodeAsync(string code, CancellationToken ct = default);

    // Cumulative balances up to and including asOf.
    Task<IReadOnlyList<AccountBalance>> GetCumulativeBalancesAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default);
    // Movement within the period only (for income/expense statements).
    Task<IReadOnlyList<AccountBalance>> GetPeriodActivityAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);
}
