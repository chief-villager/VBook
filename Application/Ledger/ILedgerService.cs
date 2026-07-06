using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;

namespace Bookkeeping.Application.Ledger;

public interface ILedgerService
{
    Task<Result> PostJournalEntryAsync(JournalEntry entry, CancellationToken ct = default);
    Task<TrialBalance> GetTrialBalanceAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default);
    Task<IReadOnlyList<AccountBalance>> GetCumulativeBalancesAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default);
    Task<IReadOnlyList<AccountBalance>> GetPeriodActivityAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);
}
