using Bookkeeping.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence;

// Shared write-side behaviour. Module repositories inherit this and add their own
// typed read queries. Saving is deferred to the unit of work (AppDbContext).
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected DbSet<T> Set => Context.Set<T>();

    public GenericRepository(AppDbContext context) => Context = context;

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await Set.AddRangeAsync(entities, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
