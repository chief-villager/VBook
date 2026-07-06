using Bookkeeping.Application.Abstractions;
using Bookkeeping.Domain.Ledger;
using Bookkeeping.Domain.Transactions.Events;

namespace Bookkeeping.Application.Ledger;

// Turns a recorded transaction into a balanced journal entry. Runs inside the
// same SaveChanges as the transaction, so both persist together or not at all.
public sealed class TransactionRecordedHandler : IDomainEventHandler<TransactionRecorded>
{
    private readonly IPostingRuleResolver _resolver;
    private readonly ILedgerRepository _ledger;

    public TransactionRecordedHandler(IPostingRuleResolver resolver, ILedgerRepository ledger)
    {
        _resolver = resolver;
        _ledger = ledger;
    }

    public async Task HandleAsync(TransactionRecorded evt, CancellationToken ct = default)
    {
        var lines = await _resolver.ResolveAsync(evt, ct);
        if (lines.IsFailure)
            throw new InvalidOperationException(lines.Error);

        var narrative = $"{evt.Type} transaction {evt.TransactionId}";
        var entry = JournalEntry.Create(evt.BusinessId, evt.OccurredOn, narrative, lines.Value.ToList(), evt.TransactionId);
        if (entry.IsFailure)
            throw new InvalidOperationException(entry.Error);

        await _ledger.AddAsync(entry.Value, ct);
    }
}
