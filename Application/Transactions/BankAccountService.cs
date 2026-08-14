using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;

namespace Bookkeeping.Application.Transactions;

public sealed class BankAccountService : IBankAccountService
{
    private readonly IBankFeedProvider _bankFeed;
    private readonly ILinkedBankAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;

    public BankAccountService(
        IBankFeedProvider bankFeed,
        ILinkedBankAccountRepository accounts,
        IUnitOfWork unitOfWork)
    {
        _bankFeed = bankFeed;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LinkedBankAccountDto>> LinkAsync(
        BusinessId businessId, string authorisationCode, CancellationToken ct = default)
    {
        // Exchange the widget's short-lived code for a durable account handle + details.
        var link = await _bankFeed.LinkAccountAsync(authorisationCode, ct);

        // Idempotent: re-linking an already-active account returns the existing record.
        var existing = await _accounts.GetActiveByExternalIdAsync(businessId, link.ExternalAccountId, ct);
        if (existing is not null)
            return ToDto(existing);

        var account = LinkedBankAccount.Link(
            businessId, link.ExternalAccountId, link.InstitutionName, link.AccountNumberMasked, link.Currency);

        await _accounts.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(account);
    }

    public async Task<IReadOnlyList<LinkedBankAccountDto>> ListAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var accounts = await _accounts.ListAsync(businessId, LinkedBankAccountStatus.Active, ct);
        return accounts.Select(ToDto).ToList();
    }

    public async Task<Result> UnlinkAsync(BusinessId businessId, LinkedBankAccountId id, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _accounts.GetAsync(id, ct), businessId, "Linked account");
        if (found.IsFailure)
            return Result.Failure(found.Error);
        var account = found.Value;

        // Disconnect at the provider first; only then mark it unlinked locally, so a
        // provider failure leaves our record untouched rather than falsely unlinked.
        await _bankFeed.UnlinkAccountAsync(account.ExternalAccountId, ct);

        var result = account.Unlink();
        if (result.IsFailure)
            return result;

        _accounts.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static LinkedBankAccountDto ToDto(LinkedBankAccount a) => new(
        a.Id, a.ExternalAccountId, a.InstitutionName, a.AccountNumberMasked, a.Currency, a.Status);
}
