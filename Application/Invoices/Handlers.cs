using Bookkeeping.Application.Abstractions;
using Bookkeeping.Domain.Invoices;
using Bookkeeping.Domain.Invoices.Events;

namespace Bookkeeping.Application.Invoices;

// Runs inside the same SaveChanges as the invoice that raised the event, so the
// outbox job and the invoice commit together. It does no external I/O — only stages
// the job. The heavy lifting (render, upload, write-back) happens later, out of the
// request path, in the background processor.
public sealed class InvoiceCreatedHandler : IDomainEventHandler<InvoiceCreated>
{
    private readonly IInvoicePdfJobRepository _jobs;

    public InvoiceCreatedHandler(IInvoicePdfJobRepository jobs) => _jobs = jobs;

    public Task HandleAsync(InvoiceCreated evt, CancellationToken ct = default)
        => _jobs.AddAsync(new InvoicePdfJob(evt.InvoiceId, evt.BusinessId), ct);
}
