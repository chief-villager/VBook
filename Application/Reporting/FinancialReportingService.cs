using Bookkeeping.Application.Ledger;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;

namespace Bookkeeping.Application.Reporting;

// Reads the Ledger through its service and groups balances into statements using
// the account tags. It owns no data and performs no accounting itself.
public sealed class FinancialReportingService : IFinancialReportingService
{
    private readonly ILedgerService _ledger;

    public FinancialReportingService(ILedgerService ledger) => _ledger = ledger;

    public async Task<ProfitAndLoss> ProfitAndLossAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
    {
        var activity = await _ledger.GetPeriodActivityAsync(businessId, period, ct);

        // Income accounts are credit-normal, so present amount = credits - debits = -balance.
        var revenue = activity
            .Where(a => a.Type == AccountType.Income)
            .Select(a => new StatementLineItem(a.StatementLine, a.Name, -a.Balance))
            .Where(i => i.Amount != 0)
            .ToList();

        var expenses = activity
            .Where(a => a.Type == AccountType.Expense)
            .Select(a => new StatementLineItem(a.StatementLine, a.Name, a.Balance))
            .Where(i => i.Amount != 0)
            .ToList();

        var totalRevenue = revenue.Sum(i => i.Amount);
        var totalExpenses = expenses.Sum(i => i.Amount);

        return new ProfitAndLoss(businessId, period, revenue, expenses,
            totalRevenue, totalExpenses, totalRevenue - totalExpenses);
    }

    public async Task<BalanceSheet> BalanceSheetAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default)
    {
        var balances = await _ledger.GetCumulativeBalancesAsync(businessId, asOf, ct);

        var assets = balances
            .Where(a => a.Type == AccountType.Asset)
            .Select(a => new StatementLineItem(a.StatementLine, a.Name, a.Balance))
            .Where(i => i.Amount != 0)
            .ToList();

        // Liabilities and equity are credit-normal: present amount = -balance.
        var liabilities = balances
            .Where(a => a.Type == AccountType.Liability)
            .Select(a => new StatementLineItem(a.StatementLine, a.Name, -a.Balance))
            .Where(i => i.Amount != 0)
            .ToList();

        var equity = balances
            .Where(a => a.Type == AccountType.Equity)
            .Select(a => new StatementLineItem(a.StatementLine, a.Name, -a.Balance))
            .Where(i => i.Amount != 0)
            .ToList();

        return new BalanceSheet(businessId, asOf, assets, liabilities, equity,
            assets.Sum(i => i.Amount), liabilities.Sum(i => i.Amount), equity.Sum(i => i.Amount));
    }

    public async Task<CashFlowStatement> CashFlowAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
    {
        // Simplified: net movement on the cash account across the period.
        var opening = await CashBalanceAsync(businessId, period.Start.AddDays(-1), ct);
        var closing = await CashBalanceAsync(businessId, period.End, ct);
        return new CashFlowStatement(businessId, period, opening, closing, closing - opening);
    }

    private async Task<decimal> CashBalanceAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct)
    {
        var balances = await _ledger.GetCumulativeBalancesAsync(businessId, asOf, ct);
        return balances
            .Where(a => a.Code == ChartOfAccounts.Cash)
            .Sum(a => a.Balance);
    }
}
