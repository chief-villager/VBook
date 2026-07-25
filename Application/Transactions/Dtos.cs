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

// Pulls a linked bank account's feed over a date range and stages the rows for review.
public sealed record PullBankFeedCommand(
    BusinessId BusinessId,
    string ExternalAccountId,
    DateRange Period);

// Result of a pull: how many rows were newly staged vs skipped as already-seen dupes.
public sealed record BankImportSummary(int Imported, int Skipped);

// A bank account a business has connected through the feed provider.
public sealed record LinkedBankAccountDto(
    LinkedBankAccountId Id,
    string ExternalAccountId,
    string InstitutionName,
    string AccountNumberMasked,
    string Currency,
    LinkedBankAccountStatus Status);

// A staged bank-feed row as shown in the review queue.
public sealed record StagedTransactionDto(
    StagedTransactionId Id,
    decimal Amount,
    DateOnly OccurredOn,
    string Narration,
    string? ProviderCategory,
    TransactionType SuggestedType,
    CategoryId? CategoryId,
    StagedTransactionStatus Status,
    TransactionId? RecordedTransactionId);
