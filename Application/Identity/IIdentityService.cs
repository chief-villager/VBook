using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public interface IIdentityService
{
    Task<Result<UserId>> RegisterUserAsync(string email, string displayName, string password, CancellationToken ct = default);
    Task<Result<BusinessId>> RegisterBusinessAsync(UserId ownerId, string name, BusinessSector sector, CancellationToken ct = default);

    // Creates the owner and their business in a single transaction, assigning the
    // owner an Owner membership.
    Task<Result<BusinessRegistrationResult>> RegisterBusinessWithOwnerAsync(
        string email, string displayName, string password,
        string businessName, BusinessSector sector, CancellationToken ct = default);

    // Provisions a new user and adds them to an existing business with the given
    // (non-owner) role, in one transaction.
    Task<Result<UserId>> AddMemberAsync(
        BusinessId businessId, string email, string displayName, string password,
        BusinessRole role, CancellationToken ct = default);

    Task<Result<IReadOnlyList<BusinessMemberDto>>> ListMembersAsync(BusinessId businessId, CancellationToken ct = default);
    Task<Result<BusinessContext>> GetBusinessAsync(BusinessId businessId, CancellationToken ct = default);
    Task<Result> EnsureOwnershipAsync(UserId userId, BusinessId businessId, CancellationToken ct = default);
    Task<Result> SetInvoiceTemplateAsync(BusinessId businessId, string logoUrl,
    string businessName, string accountNumber, string bankName, string terms,
    CancellationToken ct = default);
    Task<Result<InvoiceTemplateDto>> GetInvoiceTemplateAsync(BusinessId businessId, CancellationToken ct = default);
}
