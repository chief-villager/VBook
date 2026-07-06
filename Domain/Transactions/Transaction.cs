using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions.Events;

namespace Bookkeeping.Domain.Transactions;

public sealed class Transaction : AggregateRoot<TransactionId>
{
    public BusinessId BusinessId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Note { get; private set; }
    public bool IsVoided { get; private set; }

    private Transaction() { }

    private Transaction(BusinessId businessId, TransactionType type, decimal amount,
        CategoryId categoryId, DateOnly occurredOn, string? note)
    {
        Id = TransactionId.New();
        BusinessId = businessId;
        Type = type;
        Amount = amount;
        CategoryId = categoryId;
        OccurredOn = occurredOn;
        Note = note;
        IsVoided = false;
    }

    // Amendments go through Void then a fresh Record, never a mutating edit,
    // so the audit trail the credit signal relies on stays intact.
    public static Result<Transaction> Record(BusinessId businessId, TransactionType type, decimal amount,
        CategoryId categoryId, DateOnly occurredOn, string? note)
    {
        if (amount <= 0)
            return Result<Transaction>.Failure("Amount must be greater than zero.");

        var transaction = new Transaction(businessId, type, amount, categoryId, occurredOn, note);
        transaction.Raise(new TransactionRecorded(
            transaction.Id, businessId, type, amount, categoryId, occurredOn));
        return transaction;
    }

    public Result Void(string reason)
    {
        if (IsVoided)
            return Result.Failure("Transaction is already voided.");

        IsVoided = true;
        Raise(new TransactionVoided(Id, BusinessId, reason));
        return Result.Success();
    }
}
