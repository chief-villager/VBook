using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Ledger;

// Owned by JournalEntry. One side of a double-entry posting against a single account.
public sealed class PostingLine
{
    public AccountId AccountId { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }

    private PostingLine() { }

    public PostingLine(AccountId accountId, decimal debit, decimal credit)
    {
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
    }
}
