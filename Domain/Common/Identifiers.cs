namespace Bookkeeping.Domain.Common;

// Strongly-typed ids: a BusinessId cannot be passed where a TransactionId is expected.
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct BusinessId(Guid Value)
{
    public static BusinessId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct TransactionId(Guid Value)
{
    public static TransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct JournalEntryId(Guid Value)
{
    public static JournalEntryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.NewGuid());
    public override string ToString()
    {
        return Value.ToString();
    }
}

public readonly record struct StagedTransactionId(Guid Value)
{
    public static StagedTransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct LinkedBankAccountId(Guid Value)
{
    public static LinkedBankAccountId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct MembershipId(Guid Value)
{
    public static MembershipId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
