using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Invoices;

public interface IInvoiceService
{
    Task<Result<InvoiceId>> CreateAsync(CreateInvoiceCommand command, CancellationToken ct = default);
    Task<Result> MarkAsPaidAsync(InvoiceId id, CancellationToken ct = default);
    Task<InvoiceDetail?> GetAsync(InvoiceId id, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceSummary>> ListAsync(BusinessId businessId, CancellationToken ct = default);
}
