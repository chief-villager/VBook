using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public interface IInvoicePdfJobRepository
{
    Task AddAsync(InvoicePdfJob job, CancellationToken ct = default);

    // Returns the oldest pending job, or null when the queue is empty. "Claim" is a
    // plain read here: correct for a single processor. Running more than one instance
    // would need row-locking (e.g. UPDATE ... OUTPUT / SKIP LOCKED) to avoid double work.
    Task<InvoicePdfJob?> ClaimNextPendingAsync(CancellationToken ct = default);
}
