using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public sealed record BusinessContext(BusinessId Business, string Name, BusinessSector Sector);

// Returned by the combined registration flow: the owner and the business created
// together in one transaction.
public sealed record BusinessRegistrationResult(UserId OwnerId, BusinessId BusinessId);

// A user's membership in a business, as shown when listing a business's members.
public sealed record BusinessMemberDto(
    UserId UserId,
    string Email,
    string DisplayName,
    BusinessRole Role,
    DateTimeOffset JoinedAt);

public sealed record InvoiceTemplateDto(
    BusinessId Business,
    string LogoUrl,
    string BusinessName,
    string AccountNumber,
    string BankName,
    string Terms);
