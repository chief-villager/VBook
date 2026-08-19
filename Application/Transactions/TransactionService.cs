using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(ITransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionId>> RecordAsync(RecordTransactionCommand command, CancellationToken ct = default)
    {
        var category = await _repository.GetCategoryAsync(command.CategoryId, ct);
        if (category is null)
            return Result<TransactionId>.Failure("Category not found.");

        var result = Transaction.Record(
            command.BusinessId, category.Type, command.Amount, command.CategoryId, command.OccurredOn, command.Note);
        if (result.IsFailure)
            return Result<TransactionId>.Failure(result.Error);

        await _repository.AddAsync(result.Value, ct);

        // Saving dispatches TransactionRecorded: the Ledger posts the matching
        // balanced journal entry in the same transaction. No manual double-entry.
        await _unitOfWork.SaveChangesAsync(ct);
        return result.Value.Id;
    }

    public async Task<Result> VoidAsync(TransactionId id, string reason, CancellationToken ct = default)
    {
        var transaction = await _repository.GetAsync(id, ct);
        if (transaction is null)
            return Result.Failure("Transaction not found.");

        var result = transaction.Void(reason);
        if (result.IsFailure)
            return result;

        _repository.Update(transaction);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<TransactionSummary>> ListAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
    {
        var transactions = await _repository.ListAsync(businessId, period, ct);
        return await ToSummariesAsync(transactions, ct);
    }

    public async Task<PagedResult<TransactionSummary>> ListPagedAsync(
        BusinessId businessId, DateRange period, PageRequest page, CancellationToken ct = default)
    {
        var pageResult = await _repository.ListPagedAsync(businessId, period, page, ct);
        var items = await ToSummariesAsync(pageResult.Items, ct);
        return new PagedResult<TransactionSummary>(items, pageResult.Page, pageResult.PageSize, pageResult.TotalCount);
    }

    // Resolves each transaction's category name (shared reference data) into a summary.
    private async Task<IReadOnlyList<TransactionSummary>> ToSummariesAsync(
        IReadOnlyList<Transaction> transactions, CancellationToken ct)
    {
        var categories = (await _repository.GetCategoriesAsync(ct))
            .ToDictionary(c => c.Id, c => c.Name);

        return transactions
            .Select(t => new TransactionSummary(
                t.Id, t.Type, t.Amount,
                categories.TryGetValue(t.CategoryId, out var name) ? name : "Uncategorised",
                t.OccurredOn, t.IsVoided))
            .ToList();
    }

    // businessId is retained on the API surface but categories are now shared reference data.
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var categories = await _repository.GetCategoriesAsync(ct);
        return categories.Select(c => new CategoryDto(c.Id, c.Name, c.Type)).ToList();
    }

    public async Task<Result<Transaction>> GetEarliestTransactionDateAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var transactions = await _repository.GetEarliestTransactionDateAsync(businessId, ct);
        if (transactions is null)
            return Result<Transaction>.Failure("No transactions found for the business.");
        return transactions;
    }

  
}
