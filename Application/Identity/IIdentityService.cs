using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public interface IIdentityService
{
    Task<Result<UserId>> RegisterUserAsync(string email, string displayName, string password, CancellationToken ct = default);

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
    // Uploads the logo to object storage and stores the resulting URL. The caller
    // owns the stream; content type must be a supported image type.
    Task<Result> SetInvoiceTemplateAsync(BusinessId businessId,
    Stream logo, string logoContentType,
    string businessName, string accountNumber, string bankName, string terms,
    CancellationToken ct = default);
    Task<Result<InvoiceTemplateDto>> GetInvoiceTemplateAsync(BusinessId businessId, CancellationToken ct = default);
}
