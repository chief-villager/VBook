using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Transactions.Events;

public sealed record TransactionRecorded(
    TransactionId TransactionId,
    BusinessId BusinessId,
    TransactionType Type,
    decimal Amount,
    CategoryId CategoryId,
    DateOnly OccurredOn) : IDomainEvent;

public sealed record TransactionVoided(
    TransactionId TransactionId,
    BusinessId BusinessId,
    string Reason) : IDomainEvent;
