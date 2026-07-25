using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Transactions;

public enum StagedTransactionStatus { Pending, Approved, Discarded }

// A bank-feed row held in quarantine. External, untrusted data is persisted here and
// never touches the ledger until a human categorises and approves it, at which point
// the service promotes it into a real Transaction. Amendments are one-way status
// transitions, not edits, so the review trail stays intact.
public sealed class StagedBankTransaction : AggregateRoot<StagedTransactionId>
{
    public BusinessId BusinessId { get; private set; }
    public string ExternalAccountId { get; private set; } = null!;
    public string ExternalId { get; private set; } = null!;   // feed's own id — the dedupe key
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string Narration { get; private set; } = null!;
    public string? ProviderCategory { get; private set; }
    public TransactionType SuggestedType { get; private set; } // from Credit/Debit, advisory only
    public CategoryId? CategoryId { get; private set; }         // chosen at review
    public StagedTransactionStatus Status { get; private set; }
    public TransactionId? RecordedTransactionId { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }

    private StagedBankTransaction() { }

    public static StagedBankTransaction FromFeed(
        BusinessId businessId, string externalAccountId, string externalId, decimal amount,
        DateOnly occurredOn, string narration, string? providerCategory, TransactionType suggestedType)
        => new()
        {
            Id = StagedTransactionId.New(),
            BusinessId = businessId,
            ExternalAccountId = externalAccountId,
            ExternalId = externalId,
            Amount = amount,
            OccurredOn = occurredOn,
            Narration = narration,
            ProviderCategory = providerCategory,
            SuggestedType = suggestedType,
            Status = StagedTransactionStatus.Pending,
            ImportedAt = DateTimeOffset.UtcNow,
        };

    public Result Categorise(CategoryId categoryId)
    {
        if (Status != StagedTransactionStatus.Pending)
            return Result.Failure("Only pending imports can be categorised.");

        CategoryId = categoryId;
        return Result.Success();
    }

    // Called by the service after it has created the real Transaction, so the link and
    // the status flip commit in the same save as the Transaction itself.
    public Result MarkApproved(TransactionId recordedId)
    {
        if (Status != StagedTransactionStatus.Pending)
            return Result.Failure("Only pending imports can be approved.");
        if (CategoryId is null)
            return Result.Failure("Assign a category before approving.");

        Status = StagedTransactionStatus.Approved;
        RecordedTransactionId = recordedId;
        return Result.Success();
    }

    public Result Discard()
    {
        if (Status != StagedTransactionStatus.Pending)
            return Result.Failure("Only pending imports can be discarded.");

        Status = StagedTransactionStatus.Discarded;
        return Result.Success();
    }
}
