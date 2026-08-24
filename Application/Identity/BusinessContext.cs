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

// The authenticated caller's own profile plus every business they belong to (with the
// role that business grants them). Backs the "who am I" endpoint the SPA calls after login.
public sealed record CurrentUserDto(
    UserId UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<UserMembershipDto> Memberships);

public sealed record UserMembershipDto(
    BusinessId BusinessId,
    string BusinessName,
    BusinessRole Role,
    DateTimeOffset JoinedAt);
