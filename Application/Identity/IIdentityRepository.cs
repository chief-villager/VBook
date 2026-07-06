using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public interface IIdentityRepository : IGenericRepository<Business>
{
    Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken ct = default);
    Task<User?> GetUserAsync(UserId id, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);
}
