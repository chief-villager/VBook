using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public sealed record BusinessContext(BusinessId Business, string Name, BusinessSector Sector);
