using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

// Type is derived from the chosen category, so it is not passed in here.
public sealed record RecordTransactionCommand(
    BusinessId BusinessId,
    decimal Amount,
    CategoryId CategoryId,
    DateOnly OccurredOn,
    string? Note);

public sealed record TransactionSummary(
    TransactionId Id,
    TransactionType Type,
    decimal Amount,
    string Category,
    DateOnly OccurredOn,
    bool IsVoided);

public sealed record CategoryDto(CategoryId Id, string Name, TransactionType Type);
