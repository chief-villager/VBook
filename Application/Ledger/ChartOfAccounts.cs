namespace Bookkeeping.Application.Ledger;

// Account codes the posting rules key on. The chart of accounts itself is shared
// reference data seeded into the database (see AccountConfiguration).
public static class ChartOfAccounts
{
    public const string Cash = "1000";
    public const string SalesRevenue = "4000";
    public const string GeneralExpenses = "5000";
    public const string OwnersCapital = "3000";
    public const string LoansPayable = "2500";
}
