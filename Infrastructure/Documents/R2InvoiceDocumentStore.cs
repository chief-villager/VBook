using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Infrastructure.Documents;

// Semantic store for rendered invoice PDFs: it owns the invoice key convention and
// delegates the actual upload to the general-purpose IObjectStore, so the R2/S3
// mechanics live in one place.
public sealed class R2InvoiceDocumentStore : IInvoiceDocumentStore
{
    private readonly IObjectStore _objects;

    public R2InvoiceDocumentStore(IObjectStore objects) => _objects = objects;

    public async Task<string> SaveAsync(InvoiceId invoiceId, byte[] pdf, CancellationToken ct = default)
    {
        var key = $"invoices/{invoiceId.Value}.pdf";

        using var stream = new MemoryStream(pdf);
        return await _objects.PutAsync(key, stream, "application/pdf", ct);
    }
}
