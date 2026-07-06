namespace Bookkeeping.Application.Common;

// Generic write-side repository. Module-specific repositories extend this with
// their own typed queries (see IIdentityRepository, ITransactionRepository, etc.).
public interface IGenericRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
