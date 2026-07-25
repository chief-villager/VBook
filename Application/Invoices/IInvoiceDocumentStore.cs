using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Invoices;

// Stores a rendered invoice PDF and returns a URL to retrieve it. The application
// layer only knows the contract; the implementation (Cloudflare R2) lives in
// Infrastructure.
public interface IInvoiceDocumentStore
{
    Task<string> SaveAsync(InvoiceId invoiceId, byte[] pdf, CancellationToken ct = default);
}
