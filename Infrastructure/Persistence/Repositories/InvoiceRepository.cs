using Bookkeeping.Application.Common;
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

    public async Task<PagedResult<Invoice>> ListAsync(BusinessId businessId, PageRequest page, CancellationToken ct = default)
    {
        var query = Set.Where(i => i.BusinessId == businessId);

        var total = await query.CountAsync(ct);

        // Newest first; Id is the tiebreaker so paging is deterministic when several
        // invoices share an IssueDate.
        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Invoice>(items, page.Page, page.PageSize, total);
    }

    public Task<int> CountForBusinessAsync(BusinessId businessId, CancellationToken ct = default)
        => Set.CountAsync(i => i.BusinessId == businessId, ct);

    public Task<bool> NumberExistsAsync(BusinessId businessId, string invoiceNumber, CancellationToken ct = default)
        => Set.AnyAsync(i => i.BusinessId == businessId && i.InvoiceNumber == invoiceNumber, ct);

    public async Task<IReadOnlyCollection<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default)
    {
        return await Set.Where(i => i.BusinessId == businessId).ToListAsync(ct);
    }
}
