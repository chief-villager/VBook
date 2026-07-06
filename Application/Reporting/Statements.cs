using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Reporting;

public sealed record StatementLineItem(string StatementLine, string AccountName, decimal Amount);

public sealed record ProfitAndLoss(
    BusinessId Business,
    DateRange Period,
    IReadOnlyList<StatementLineItem> Revenue,
    IReadOnlyList<StatementLineItem> Expenses,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetProfit);

public sealed record BalanceSheet(
    BusinessId Business,
    DateOnly AsOf,
    IReadOnlyList<StatementLineItem> Assets,
    IReadOnlyList<StatementLineItem> Liabilities,
    IReadOnlyList<StatementLineItem> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity);

public sealed record CashFlowStatement(
    BusinessId Business,
    DateRange Period,
    decimal OpeningCash,
    decimal ClosingCash,
    decimal NetChange);
