using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Transactions;

public enum LinkedBankAccountStatus { Active, Unlinked }

// A bank account a business has connected through the feed provider. Holds the
// provider's durable account handle plus display details; unlinking is a one-way
// status transition so the record (and its link to imported rows) is preserved.
public sealed class LinkedBankAccount : AggregateRoot<LinkedBankAccountId>
{
    public BusinessId BusinessId { get; private set; }
    public string ExternalAccountId { get; private set; } = null!;  // provider's durable id
    public string InstitutionName { get; private set; } = null!;
    public string AccountNumberMasked { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public LinkedBankAccountStatus Status { get; private set; }
    public DateTimeOffset LinkedAt { get; private set; }

    private LinkedBankAccount() { }

    public static LinkedBankAccount Link(
        BusinessId businessId, string externalAccountId, string institutionName,
        string accountNumberMasked, string currency)
        => new()
        {
            Id = LinkedBankAccountId.New(),
            BusinessId = businessId,
            ExternalAccountId = externalAccountId,
            InstitutionName = institutionName,
            AccountNumberMasked = accountNumberMasked,
            Currency = currency,
            Status = LinkedBankAccountStatus.Active,
            LinkedAt = DateTimeOffset.UtcNow,
        };

    public Result Unlink()
    {
        if (Status == LinkedBankAccountStatus.Unlinked)
            return Result.Failure("Account is already unlinked.");

        Status = LinkedBankAccountStatus.Unlinked;
        return Result.Success();
    }
}
