using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;

namespace Bookkeeping.Application.Ledger;

public sealed class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public LedgerService(ILedgerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> PostJournalEntryAsync(JournalEntry entry, CancellationToken ct = default)
    {
        await _repository.AddAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<TrialBalance> GetTrialBalanceAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default)
    {
        var balances = await _repository.GetCumulativeBalancesAsync(businessId, asOf, ct);
        var totalDebits = balances.Where(b => b.Balance > 0).Sum(b => b.Balance);
        var totalCredits = balances.Where(b => b.Balance < 0).Sum(b => -b.Balance);
        return new TrialBalance(businessId, asOf, balances, totalDebits, totalCredits);
    }

    public Task<IReadOnlyList<AccountBalance>> GetCumulativeBalancesAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default)
        => _repository.GetCumulativeBalancesAsync(businessId, asOf, ct);

    public Task<IReadOnlyList<AccountBalance>> GetPeriodActivityAsync(BusinessId businessId, DateRange period, CancellationToken ct = default)
        => _repository.GetPeriodActivityAsync(businessId, period, ct);
}
