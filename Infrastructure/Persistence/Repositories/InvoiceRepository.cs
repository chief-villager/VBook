using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext context) : base(context) { }

    // Line items are an owned collection, so EF loads them with the invoice; no Include needed.
    public Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct = default)
    {
        return Set.FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<IReadOnlyList<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default)
        => await Set
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(ct);

    public Task<bool> NumberExistsAsync(BusinessId businessId, string invoiceNumber, CancellationToken ct = default)
        => Set.AnyAsync(i => i.BusinessId == businessId && i.InvoiceNumber == invoiceNumber, ct);
}
