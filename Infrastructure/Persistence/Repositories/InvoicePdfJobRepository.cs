using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class InvoicePdfJobRepository : IInvoicePdfJobRepository
{
    private readonly AppDbContext _context;

    public InvoicePdfJobRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(InvoicePdfJob job, CancellationToken ct = default)
        => await _context.Set<InvoicePdfJob>().AddAsync(job, ct);

    /// <summary>
    /// This method is used to get the first invoicepdfjob thats pending in order 
    /// their created date.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<InvoicePdfJob?> ClaimNextPendingAsync(CancellationToken ct = default)
        => _context.Set<InvoicePdfJob>()
            .Where(j => j.Status == InvoicePdfJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
