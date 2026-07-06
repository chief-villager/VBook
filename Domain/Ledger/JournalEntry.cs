using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Ledger;

// The heart of the system. A balanced set of posting lines. The invariant that
// debits equal credits is enforced here and can never be bypassed by a caller.
public sealed class JournalEntry : AggregateRoot<JournalEntryId>
{
    public BusinessId BusinessId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Narrative { get; private set; } = string.Empty;
    public TransactionId? SourceTransactionId { get; private set; }

    private readonly List<PostingLine> _lines = new();
    public IReadOnlyCollection<PostingLine> Lines => _lines.AsReadOnly();

    private JournalEntry() { }

    private JournalEntry(BusinessId businessId, DateOnly date, string narrative,
        IEnumerable<PostingLine> lines, TransactionId? sourceTransactionId)
    {
        Id = JournalEntryId.New();
        BusinessId = businessId;
        Date = date;
        Narrative = narrative;
        SourceTransactionId = sourceTransactionId;
        _lines.AddRange(lines);
    }

    public static Result<JournalEntry> Create(BusinessId businessId, DateOnly date, string narrative,
        IReadOnlyList<PostingLine> lines, TransactionId? sourceTransactionId = null)
    {
        if (lines.Count < 2)
            return Result<JournalEntry>.Failure("A journal entry needs at least two posting lines.");

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            return Result<JournalEntry>.Failure(
                $"Unbalanced entry: debits {totalDebit:N2} do not equal credits {totalCredit:N2}.");

        if (totalDebit == 0m)
            return Result<JournalEntry>.Failure("A journal entry cannot be for a zero amount.");

        return new JournalEntry(businessId, date, narrative, lines, sourceTransactionId);
    }
}
