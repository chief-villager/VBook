using Bookkeeping.Application.Reporting;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.CreditReadiness;

public enum CreditFactor { Character, Capacity, Capital, Collateral, Conditions }

public enum GapKind { NoAssetsRecorded, ShortHistory, IrregularRecording, LowCategorisation, NegativeCashFlow }

public sealed record EvidenceItem(string Label, string Value, string Source);

public sealed record FactorEvidence(CreditFactor Factor, IReadOnlyList<EvidenceItem> Items);

public sealed record ReadinessGap(GapKind Kind, string Detail, string SuggestedAction);

public sealed record RecordKeepingSummary(
    DateRange Coverage,
    int MonthsWithActivity,
    int MonthsInWindow,
    decimal ActiveTransactionShare);

// No score, no verdict. This is documentation arranged for a lender to read.
public sealed record CreditReadinessReport(
    BusinessId Business,
    DateRange Window,
    ProfitAndLoss ProfitAndLoss,
    BalanceSheet BalanceSheet,
    CashFlowStatement CashFlow,
    RecordKeepingSummary Records,
    IReadOnlyList<FactorEvidence> FiveCs,
    IReadOnlyList<ReadinessGap> Gaps);

public sealed record DataSufficiency(bool IsSufficient, IReadOnlyList<string> Missing);

public sealed record CreditReadinessDashBoard(
    BusinessId Business,
    DateRange Window,
    int NumberOfTransactions,
    int MonthsOfHistory,
    int NumberOfInvoices);

public enum Rating
{
    Weak = 25,
    ModerateSignal = 50,
    StrongSignal = 75,
    VeryStrongSignal = 100,
    NotObservable = 0
}
public sealed record FiveCsRating( 
   List<CreditFactorRating> Ratings,
   string recordKeepingScore,
   string obeservableStrengthScore
   
);

public sealed record CreditFactorRating(
    CreditFactor Factor,
    Rating Rating,
    string Description,
    string SuggestedAction,
    int Score
);