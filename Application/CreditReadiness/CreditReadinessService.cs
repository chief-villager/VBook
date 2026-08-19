using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Invoices;
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
    private readonly IInvoiceService _invoices;

    public CreditReadinessService(
        IFinancialReportingService reporting,
        ITransactionService transactions,
        IIdentityService identity,
        IInvoiceService invoices)
    {
        _reporting = reporting;
        _transactions = transactions;
        _identity = identity;
        _invoices = invoices;
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

    public async Task<Result<CreditReadinessDashBoard>> CreditReadinessDashBoard(BusinessId businessId, DateRange window, CancellationToken ct = default)
    {
        var transactions = await _transactions.ListAsync(businessId, window, ct);
        var NumberOfTransactions = transactions.Count;
        var earliestTransactionDate = await _transactions.GetEarliestTransactionDateAsync(businessId, ct);
        // No transactions yet -> no history to measure, so report zero months rather than throw.
        var months = earliestTransactionDate.IsSuccess
            ? MonthsBetween(earliestTransactionDate.Value.OccurredOn, window.End)
            : 0;
        var invoices = await _invoices.ListAsync(businessId, ct);
        var invoicesCount = invoices.Count;

        var dashboard = new CreditReadinessDashBoard(
            Business: businessId,
            Window: window,
            NumberOfTransactions: NumberOfTransactions,
            MonthsOfHistory: months,
            NumberOfInvoices: invoicesCount
        );

        return Result<CreditReadinessDashBoard>.Success(dashboard);
    }

    public async Task<Result<List<FiveCsRating>>> EvaluateCreditReadinessAsync(BusinessId businessId, DateRange window, CancellationToken ct = default)
    {   
       var report = await GenerateReportAsync(businessId, window, ct);
        if (report.IsFailure)
            return Result<List<FiveCsRating>>.Failure(report.Error);

      if (report.Value.Records.MonthsWithActivity == 0)
      {
            var character = new CreditFactorRating(CreditFactor.Character, Rating.Weak, "Insufficient history to assess creditworthiness.", "Keep recording consistently to build a track record.", 0);
            var capacity = await CapacityRatingAsync(report.Value.ProfitAndLoss, report.Value.CashFlow, ct);
            var capital = await CapitalRatingAsync(report.Value.BalanceSheet, ct);
            var collateral = await CollateralRatingAsync(report.Value.BalanceSheet, ct);
            var completeness = CompletenessCalculation(capacity.Score, character.Score, capital.Score, collateral.Score);
            var result = new FiveCsRating(new List<CreditFactorRating> { character, capacity, capital, collateral },  completeness.recordKeepingScore.ToString(), completeness.observableStrengthScore.ToString());
            return Result<List<FiveCsRating>>.Success(new List<FiveCsRating> { result });
            // return Result<List<FiveCsRating>>.Failure("No transactions found for the business in the specified window.");
      }

      if (report.Value.Records.MonthsWithActivity >0 && report.Value.Records.MonthsWithActivity <= MinMonthsOfHistory)
        {
            var character = new CreditFactorRating(CreditFactor.Character, Rating.Weak, "Insufficient history to assess creditworthiness.", "Keep recording consistently to build a track record.", 25);
            var capacity = await CapacityRatingAsync(report.Value.ProfitAndLoss, report.Value.CashFlow, ct);
            var capital = await CapitalRatingAsync(report.Value.BalanceSheet, ct);
            var collateral = await CollateralRatingAsync(report.Value.BalanceSheet, ct);
            var completeness = CompletenessCalculation(capacity.Score, character.Score, capital.Score, collateral.Score);
            var result = new FiveCsRating(new List<CreditFactorRating> { character, capacity, capital, collateral },  completeness.recordKeepingScore.ToString(), completeness.observableStrengthScore.ToString());
            return Result<List<FiveCsRating>>.Success(new List<FiveCsRating> { result });
        }

        if (report.Value.Records.MonthsWithActivity > MinMonthsOfHistory && report.Value.Records.MonthsWithActivity <= 6)
        {
            var character = new CreditFactorRating(CreditFactor.Character, Rating.ModerateSignal, "Limited history available for assessment.", "Continue recording to improve credit evaluation.", 50);
            var capacity = await CapacityRatingAsync(report.Value.ProfitAndLoss, report.Value.CashFlow, ct);
            var capital = await CapitalRatingAsync(report.Value.BalanceSheet, ct);
            var collateral = await CollateralRatingAsync(report.Value.BalanceSheet, ct);
            var completeness = CompletenessCalculation(capacity.Score, character.Score, capital.Score, collateral.Score);
            var result = new FiveCsRating(new List<CreditFactorRating> { character, capacity, capital, collateral },  completeness.recordKeepingScore.ToString(), completeness.observableStrengthScore.ToString());
            return Result<List<FiveCsRating>>.Success(new List<FiveCsRating> { result });
        }

        if (report.Value.Records.MonthsWithActivity > 6 && report.Value.Records.MonthsWithActivity <= 9)
        {
            var character = new CreditFactorRating(CreditFactor.Character, Rating.StrongSignal, "Moderate history available for assessment.", "Maintain consistent recording to further improve credit evaluation.", 75);
            var capacity = await CapacityRatingAsync(report.Value.ProfitAndLoss, report.Value.CashFlow, ct);
            var capital = await CapitalRatingAsync(report.Value.BalanceSheet, ct);
            var collateral = await CollateralRatingAsync(report.Value.BalanceSheet, ct);
            var completeness = CompletenessCalculation(capacity.Score, character.Score, capital.Score, collateral.Score);
            var result = new FiveCsRating(new List<CreditFactorRating> { character, capacity, capital, collateral },  completeness.recordKeepingScore.ToString(), completeness.observableStrengthScore.ToString());
            return Result<List<FiveCsRating>>.Success(new List<FiveCsRating> { result });
        }
        if (report.Value.Records.MonthsWithActivity > 9)
        {
            var character = new CreditFactorRating(CreditFactor.Character, Rating.VeryStrongSignal, "Extensive history available for assessment.", "Continue maintaining consistent recording to maximize credit evaluation.", 100);
            var capacity = await CapacityRatingAsync(report.Value.ProfitAndLoss, report.Value.CashFlow, ct);
            var capital = await CapitalRatingAsync(report.Value.BalanceSheet, ct);
            var collateral = await CollateralRatingAsync(report.Value.BalanceSheet, ct);
            var completeness = CompletenessCalculation(capacity.Score, character.Score, capital.Score, collateral.Score);
            var result = new FiveCsRating(new List<CreditFactorRating> { character, capacity, capital, collateral },  completeness.recordKeepingScore.ToString(), completeness.observableStrengthScore.ToString());
            return Result<List<FiveCsRating>>.Success(new List<FiveCsRating> { result });
        }
        
        return Result<List<FiveCsRating>>.Failure("Unable to evaluate credit readiness.");
    
          
    }

    private Task<CreditFactorRating> CapacityRatingAsync( ProfitAndLoss pnl, CashFlowStatement cashFlow, CancellationToken ct = default)
    {
        if (pnl.NetProfit > 0 && cashFlow.NetChange > 0)
        {
            return Task.FromResult(new CreditFactorRating(CreditFactor.Capacity, Rating.StrongSignal, "The business has a positive net profit and cash flow, indicating strong capacity to meet financial obligations.", "Maintain profitability and positive cash flow to sustain creditworthiness.", 75));
        }
        else if (pnl.NetProfit > 0 && cashFlow.NetChange <= 0)
        {
            return Task.FromResult(new CreditFactorRating(CreditFactor.Capacity, Rating.ModerateSignal, "The business has a positive net profit but negative cash flow, indicating moderate capacity to meet financial obligations.", "Improve cash flow management to enhance creditworthiness.", 50));
        }
      
        return Task.FromResult(new CreditFactorRating(CreditFactor.Capacity, Rating.Weak, "The business has negative net profit and/or negative cash flow, indicating weak capacity to meet financial obligations.", "Take steps to improve profitability and cash flow to enhance creditworthiness.", 0));
    }

    private Task<CreditFactorRating> CapitalRatingAsync(BalanceSheet balanceSheet, CancellationToken ct = default)
    {
        if (balanceSheet.TotalEquity > 0 && balanceSheet.TotalAssets > 0 )
        {
            return Task.FromResult(new CreditFactorRating(CreditFactor.Capital, Rating.StrongSignal, "The business has sufficient equity and assets, indicating strong capital structure.", "Maintain a healthy capital structure to support creditworthiness.", 75));
        }
        else if (balanceSheet.TotalEquity > 0 && balanceSheet.TotalAssets == 0)
        {
            return Task.FromResult(new CreditFactorRating(CreditFactor.Capital, Rating.ModerateSignal, "The business has sufficient equity but no assets, indicating moderate capital structure.", "Improve asset base to enhance creditworthiness.", 50));
        }
      
        return Task.FromResult(new CreditFactorRating(CreditFactor.Capital, Rating.Weak, "The business has insufficient equity and/or assets, indicating weak capital structure.", "Take steps to improve equity and asset base to enhance creditworthiness.", 0));
    }

    private Task<CreditFactorRating> CollateralRatingAsync(BalanceSheet balanceSheet, CancellationToken ct = default)
    {
        if (balanceSheet.TotalAssets > 0 )
        {
            return Task.FromResult(new CreditFactorRating(CreditFactor.Collateral, Rating.NotObservable, "Lenders assess collateral based on asset value and quality.", "Maintain accurate asset records to support collateral evaluation.", 0));
        }
      
        return Task.FromResult(new CreditFactorRating(CreditFactor.Collateral, Rating.NotObservable, "Lenders assess collateral based on asset value and quality.", "Maintain accurate asset records to support collateral evaluation.", 0));
    }

    private (int recordKeepingScore, int observableStrengthScore) CompletenessCalculation(int capacity, int character, int capital, int collateralandconditions)
    {
        int totalFactors = 4;
        int observableStrengthScore = (capacity + character + capital + collateralandconditions) / totalFactors;
        int recordKeepingScore = character;
        return (recordKeepingScore, observableStrengthScore);
    }
    
   
    
    private static int MonthsBetween(DateOnly start, DateOnly end) =>
        ((end.Year - start.Year) * 12) + end.Month - start.Month + 1;
}
