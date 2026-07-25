using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Invoices;

public enum InvoicePdfJobStatus
{
    Pending,
    Done,
    Failed
}

// The outbox row for invoice PDF generation. It is written in the same transaction
// as the invoice, so "an invoice exists" and "a PDF is owed for it" commit together
// or not at all. A background processor drains pending rows out of the request path.
public sealed class InvoicePdfJob
{
    // Give up after this many failed attempts so a permanently broken job (bad
    // template, storage misconfiguration) does not retry forever.
    private const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public InvoiceId InvoiceId { get; private set; }
    public BusinessId BusinessId { get; private set; }
    public InvoicePdfJobStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private InvoicePdfJob() { } // EF

    public InvoicePdfJob(InvoiceId invoiceId, BusinessId businessId)
    {
        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        BusinessId = businessId;
        Status = InvoicePdfJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkDone()
    {
        Status = InvoicePdfJobStatus.Done;
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error;
        // Stays Pending (and so is retried) until the attempt ceiling is hit.
        if (Attempts >= MaxAttempts)
        {
            Status = InvoicePdfJobStatus.Failed;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
