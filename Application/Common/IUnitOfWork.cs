namespace Bookkeeping.Application.Common;

// One save point across all modules. Calling this also dispatches any domain
// events raised during the operation, so everything commits atomically.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
