using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Reporting;
using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.CreditReadiness;

// Reads Reporting and Transactions and arranges the evidence under the 5Cs.
// It computes descriptive record metrics but never a creditworthiness score.
public sealed class CreditReadinessService : ICreditReadinessService
{
    private const int MinMonthsOfHistory = 3;

    private readonly IFinancialReportingService _reporting;
    private readonly ITransactionService _transactions;
    private readonly IIdentityService _identity;

    public CreditReadinessService(
        IFinancialReportingService reporting,
        ITransactionService transactions,
        IIdentityService identity)
    {
        _reporting = reporting;
        _transactions = transactions;
        _identity = identity;
    }

    public async Task<DataSufficiency> CheckDataSufficiencyAsync(BusinessId businessId, DateRange window, CancellationToken ct = default)
    {
        var records = await BuildRecordSummaryAsync(businessId, window, ct);
        var missing = new List<string>();

        if (records.MonthsWithActivity < MinMonthsOfHistory)
            missing.Add($"At least {MinMonthsOfHistory} months of recorded activity (currently {records.MonthsWithActivity}).");

        return new DataSufficiency(missing.Count == 0, missing);
    }

    public async Task<Result<CreditReadinessReport>> GenerateReportAsync(BusinessId businessId, DateRange window, CancellationToken ct = default)
    {
        var business = await _identity.GetBusinessAsync(businessId, ct);
        if (business.IsFailure)
            return Result<CreditReadinessReport>.Failure(business.Error);

        var pnl = await _reporting.ProfitAndLossAsync(businessId, window, ct);
        var balanceSheet = await _reporting.BalanceSheetAsync(businessId, window.End, ct);
        var cashFlow = await _reporting.CashFlowAsync(businessId, window, ct);
        var records = await BuildRecordSummaryAsync(businessId, window, ct);

        var fiveCs = BuildFiveCs(pnl, balanceSheet, records, business.Value);
        var gaps = DetectGaps(pnl, balanceSheet, cashFlow, records);

        return new CreditReadinessReport(
            businessId, window, pnl, balanceSheet, cashFlow, records, fiveCs, gaps);
    }

    private async Task<RecordKeepingSummary> BuildRecordSummaryAsync(BusinessId businessId, DateRange window, CancellationToken ct)
    {
        var transactions = await _transactions.ListAsync(businessId, window, ct);

        var monthsWithActivity = transactions
            .Where(t => !t.IsVoided)
            .Select(t => new DateOnly(t.OccurredOn.Year, t.OccurredOn.Month, 1))
            .Distinct()
            .Count();

        var monthsInWindow = MonthsBetween(window.Start, window.End);
        var activeShare = transactions.Count == 0
            ? 0m
            : Math.Round((decimal)transactions.Count(t => !t.IsVoided) / transactions.Count, 2);

        return new RecordKeepingSummary(window, monthsWithActivity, monthsInWindow, activeShare);
    }

    private static IReadOnlyList<FactorEvidence> BuildFiveCs(
        ProfitAndLoss pnl, BalanceSheet balanceSheet, RecordKeepingSummary records, BusinessContext business)
    {
        var character = new FactorEvidence(CreditFactor.Character, new List<EvidenceItem>
        {
            new("Months with recorded activity", $"{records.MonthsWithActivity} of {records.MonthsInWindow}", "Transactions"),
            new("Active (non-voided) share", records.ActiveTransactionShare.ToString("P0"), "Transactions"),
        });

        var capacity = new FactorEvidence(CreditFactor.Capacity, new List<EvidenceItem>
        {
            new("Total revenue", pnl.TotalRevenue.ToString("N2"), "Profit and loss"),
            new("Total expenses", pnl.TotalExpenses.ToString("N2"), "Profit and loss"),
            new("Net surplus", pnl.NetProfit.ToString("N2"), "Profit and loss"),
        });

        var capital = new FactorEvidence(CreditFactor.Capital, new List<EvidenceItem>
        {
            new("Owner's equity", balanceSheet.TotalEquity.ToString("N2"), "Balance sheet"),
        });

        var collateral = new FactorEvidence(CreditFactor.Collateral, balanceSheet.Assets
            .Select(a => new EvidenceItem(a.AccountName, a.Amount.ToString("N2"), "Balance sheet"))
            .DefaultIfEmpty(new EvidenceItem("Recorded assets", "0.00", "Balance sheet"))
            .ToList());

        var conditions = new FactorEvidence(CreditFactor.Conditions, new List<EvidenceItem>
        {
            new("Sector", business.Sector.ToString(), "Identity"),
            new("Trading period covered", records.Coverage.ToString(), "Report window"),
        });

        return new List<FactorEvidence> { character, capacity, capital, collateral, conditions };
    }

    private static IReadOnlyList<ReadinessGap> DetectGaps(
        ProfitAndLoss pnl, BalanceSheet balanceSheet, CashFlowStatement cashFlow, RecordKeepingSummary records)
    {
        var gaps = new List<ReadinessGap>();

        if (balanceSheet.TotalAssets == 0)
            gaps.Add(new ReadinessGap(GapKind.NoAssetsRecorded,
                "No business assets are recorded, so collateral cannot be evidenced.",
                "Record owned equipment, inventory, or property."));

        if (records.MonthsWithActivity < MinMonthsOfHistory)
            gaps.Add(new ReadinessGap(GapKind.ShortHistory,
                $"Only {records.MonthsWithActivity} months of activity are recorded.",
                "Keep recording consistently to build a track record."));

        if (records.MonthsInWindow > 0 && records.MonthsWithActivity < records.MonthsInWindow)
            gaps.Add(new ReadinessGap(GapKind.IrregularRecording,
                "Some months in the window have no recorded activity.",
                "Record transactions every month, even quiet ones."));

        if (cashFlow.NetChange < 0)
            gaps.Add(new ReadinessGap(GapKind.NegativeCashFlow,
                "Cash decreased over the period.",
                "A lender will want to see how repayments would be serviced."));

        return gaps;
    }

    private static int MonthsBetween(DateOnly start, DateOnly end) =>
        ((end.Year - start.Year) * 12) + end.Month - start.Month + 1;
}
