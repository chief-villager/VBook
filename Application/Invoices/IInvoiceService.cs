using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public interface IInvoiceService
{
    Task<Result<InvoiceId>> CreateAsync(CreateInvoiceCommand command, CancellationToken ct = default);
    // businessId scopes the resource: the invoice must belong to it, else "not found".
    Task<Result> MarkAsPaidAsync(BusinessId businessId, InvoiceId id, CancellationToken ct = default);
    Task<InvoiceDetail?> GetAsync(BusinessId businessId, InvoiceId id, CancellationToken ct = default);
    Task<PagedResult<InvoiceSummary>> ListAsync(BusinessId businessId, PageRequest page, CancellationToken ct = default);
    Task<IReadOnlyCollection<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default);
}
