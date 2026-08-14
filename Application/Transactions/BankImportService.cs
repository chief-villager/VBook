using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public sealed class BankImportService : IBankImportService
{
    private readonly IBankFeedProvider _bankFeed;
    private readonly IStagedBankTransactionRepository _staged;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public BankImportService(
        IBankFeedProvider bankFeed,
        IStagedBankTransactionRepository staged,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork)
    {
        _bankFeed = bankFeed;
        _staged = staged;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BankImportSummary>> PullAsync(PullBankFeedCommand command, CancellationToken ct = default)
    {
        var feed = await _bankFeed.GetTransactionsAsync(
            command.ExternalAccountId, command.Period.Start, command.Period.End, ct);

        var known = await _staged.ExistingExternalIdsAsync(
            command.BusinessId, feed.Select(t => t.ExternalId), ct);

        var fresh = feed
            .Where(t => !known.Contains(t.ExternalId))
            .Select(t => StagedBankTransaction.FromFeed(
                command.BusinessId,
                command.ExternalAccountId,
                t.ExternalId,
                t.Amount,
                DateOnly.FromDateTime(t.OccurredAt.Date),
                t.Narration,
                t.ProviderCategory,
                // Credit = money into the account (income); Debit = money out (expense).
                t.Direction == BankFeedDirection.Credit ? TransactionType.Income : TransactionType.Expense))
            .ToList();

        await _staged.AddRangeAsync(fresh, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new BankImportSummary(Imported: fresh.Count, Skipped: feed.Count - fresh.Count);
    }

    public async Task<PagedResult<StagedTransactionDto>> ListAsync(
        BusinessId businessId, StagedTransactionStatus status, PageRequest page, CancellationToken ct = default)
    {
        var staged = await _staged.ListAsync(businessId, status, page, ct);
        var items = staged.Items
            .Select(s => new StagedTransactionDto(
                s.Id, s.Amount, s.OccurredOn, s.Narration, s.ProviderCategory,
                s.SuggestedType, s.CategoryId, s.Status, s.RecordedTransactionId))
            .ToList();

        return new PagedResult<StagedTransactionDto>(items, staged.Page, staged.PageSize, staged.TotalCount);
    }

    public async Task<Result> CategoriseAsync(BusinessId businessId, StagedTransactionId id, CategoryId categoryId, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _staged.GetAsync(id, ct), businessId, "Staged import");
        if (found.IsFailure)
            return Result.Failure(found.Error);
        var staged = found.Value;

        var category = await _transactions.GetCategoryAsync(categoryId, ct);
        if (category is null)
            return Result.Failure("Category not found.");

        var result = staged.Categorise(categoryId);
        if (result.IsFailure)
            return result;

        _staged.Update(staged);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // Promotes a reviewed row into a real Transaction. The new Transaction, its
    // TransactionRecorded event (→ balanced journal entry), and the staged status
    // flip all commit in a single SaveChanges, so approval is atomic: no orphaned
    // Transaction and no double-record if the process dies mid-way.
    public async Task<Result<TransactionId>> ApproveAsync(BusinessId businessId, StagedTransactionId id, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _staged.GetAsync(id, ct), businessId, "Staged import");
        if (found.IsFailure)
            return Result<TransactionId>.Failure(found.Error);
        var staged = found.Value;

        if (staged.CategoryId is null)
            return Result<TransactionId>.Failure("Assign a category before approving.");

        var category = await _transactions.GetCategoryAsync(staged.CategoryId.Value, ct);
        if (category is null)
            return Result<TransactionId>.Failure("Category not found.");

        var record = Transaction.Record(
            staged.BusinessId, category.Type, staged.Amount, staged.CategoryId.Value,
            staged.OccurredOn, note: $"Bank import {staged.ExternalId}");
        if (record.IsFailure)
            return Result<TransactionId>.Failure(record.Error);

        await _transactions.AddAsync(record.Value, ct);

        var approved = staged.MarkApproved(record.Value.Id);
        if (approved.IsFailure)
            return Result<TransactionId>.Failure(approved.Error);
        _staged.Update(staged);

        await _unitOfWork.SaveChangesAsync(ct);
        return record.Value.Id;
    }

    public async Task<Result> DiscardAsync(BusinessId businessId, StagedTransactionId id, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _staged.GetAsync(id, ct), businessId, "Staged import");
        if (found.IsFailure)
            return Result.Failure(found.Error);
        var staged = found.Value;

        var result = staged.Discard();
        if (result.IsFailure)
            return result;

        _staged.Update(staged);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
