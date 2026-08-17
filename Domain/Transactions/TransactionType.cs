namespace Bookkeeping.Domain.Transactions;

public enum TransactionType
{
    Income,
    Expense,
    Capital,
    Loan
}

public static class TransactionTypeExtensions
{
    // The cash effect each type has in the ledger (mirrors PostingRuleResolver, which
    // is the authority): income, owner capital injections and loan drawdowns all debit
    // cash — money coming in — while an expense credits cash — money going out. Used to
    // check a chosen category against a bank row's debit/credit direction.
    public static bool IsCashInflow(this TransactionType type) => type switch
    {
        TransactionType.Income => true,
        TransactionType.Capital => true,
        TransactionType.Loan => true,
        TransactionType.Expense => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown transaction type."),
    };
}
