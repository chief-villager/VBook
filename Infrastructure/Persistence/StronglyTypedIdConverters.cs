using Bookkeeping.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookkeeping.Infrastructure.Persistence;

// Teach EF Core to store each strongly-typed id as a Guid column.
public sealed class UserIdConverter : ValueConverter<UserId, Guid>
{
    public UserIdConverter() : base(id => id.Value, value => new UserId(value)) { }
}

public sealed class BusinessIdConverter : ValueConverter<BusinessId, Guid>
{
    public BusinessIdConverter() : base(id => id.Value, value => new BusinessId(value)) { }
}

public sealed class TransactionIdConverter : ValueConverter<TransactionId, Guid>
{
    public TransactionIdConverter() : base(id => id.Value, value => new TransactionId(value)) { }
}

public sealed class NullableTransactionIdConverter : ValueConverter<TransactionId?, Guid?>
{
    public NullableTransactionIdConverter()
        : base(id => id == null ? (Guid?)null : id.Value.Value,
               value => value == null ? (TransactionId?)null : new TransactionId(value.Value)) { }
}

public sealed class CategoryIdConverter : ValueConverter<CategoryId, Guid>
{
    public CategoryIdConverter() : base(id => id.Value, value => new CategoryId(value)) { }
}

public sealed class AccountIdConverter : ValueConverter<AccountId, Guid>
{
    public AccountIdConverter() : base(id => id.Value, value => new AccountId(value)) { }
}

public sealed class JournalEntryIdConverter : ValueConverter<JournalEntryId, Guid>
{
    public JournalEntryIdConverter() : base(id => id.Value, value => new JournalEntryId(value)) { }
}
