using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public sealed record BusinessContext(BusinessId Business, string Name, BusinessSector Sector);

public sealed record InvoiceTemplateDto(
    BusinessId Business,
    string LogoUrl,
    string BusinessName,
    string AccountNumber,
    string BankName,
    string Terms);
