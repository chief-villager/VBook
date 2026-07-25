using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public interface IIdentityRepository : IGenericRepository<Business>
{
    Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken ct = default);
    Task<InvoiceTemplate?> GetBusinessInvoiceTemplateAsync(BusinessId id, CancellationToken ct = default);
    Task<User?> GetUserAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);

    Task AddMembershipAsync(BusinessMembership membership, CancellationToken ct = default);
    Task<BusinessMembership?> GetMembershipAsync(BusinessId businessId, UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<BusinessMembership>> ListMembershipsAsync(BusinessId businessId, CancellationToken ct = default);
    Task<IReadOnlyList<BusinessMembership>> ListMembershipsForUserAsync(UserId userId, CancellationToken ct = default);
}
