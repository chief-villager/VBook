using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public interface IInvoiceRepository : IGenericRepository<Invoice>
{
    Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct = default);

    // One page of the business's invoices, newest first, with the total count.
    Task<PagedResult<Invoice>> ListAsync(BusinessId businessId, PageRequest page, CancellationToken ct = default);
    Task<IReadOnlyCollection<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default);


    // Invoice numbers are assigned server-side and unique per business. The service
    // derives the next sequence from the current count and guards the rare collision
    // with NumberExistsAsync (backed by a unique index on BusinessId + InvoiceNumber).
    Task<int> CountForBusinessAsync(BusinessId businessId, CancellationToken ct = default);

    Task<bool> NumberExistsAsync(BusinessId businessId, string invoiceNumber, CancellationToken ct = default);
}
