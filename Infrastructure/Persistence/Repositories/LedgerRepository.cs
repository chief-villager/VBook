using Bookkeeping.Application.Ledger;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class LedgerRepository : GenericRepository<JournalEntry>, ILedgerRepository
{
    public LedgerRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default)
        => await Context.Set<Account>()
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

    public Task<Account?> GetAccountByCodeAsync(string code, CancellationToken ct = default)
        => Context.Set<Account>().FirstOrDefaultAsync(a => a.Code == code, ct);

    public async Task<IReadOnlyList<AccountBalance>> GetCumulativeBalancesAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default)
    {
        var accounts = await GetAccountsAsync(ct);
        var entries = await Set
            .Where(j => j.BusinessId == businessId && j.Date <= asOf)
            .ToListAsync(ct);
        return Aggregate(accounts, entries);
    }

    public async Task<IReadOnlyList<AccountBalance>> GetPeriodActivityAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
    {
        var accounts = await GetAccountsAsync(ct);
        var entries = await Set
            .Where(j => j.BusinessId == businessId && j.Date >= period.Start && j.Date <= period.End)
            .ToListAsync(ct);
        return Aggregate(accounts, entries);
    }

    // Owned posting lines load with their entry, so balances are summed in memory.
    // Fine at dissertation scale; a SQL-side GROUP BY is the noted scaling path.
    private static IReadOnlyList<AccountBalance> Aggregate(IReadOnlyList<Account> accounts, IReadOnlyList<JournalEntry> entries)
    {
        var sums = new Dictionary<AccountId, decimal>();
        foreach (var entry in entries)
            foreach (var line in entry.Lines)
                sums[line.AccountId] = sums.GetValueOrDefault(line.AccountId) + line.Debit - line.Credit;

        return accounts
            .Select(a => new AccountBalance(
                a.Id, a.Code, a.Name, a.Type, a.StatementLine,
                sums.GetValueOrDefault(a.Id)))
            .ToList();
    }
}
