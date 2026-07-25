using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Invoices.Events;

// Raised when an invoice is created. Its handler stages an outbox job (in the same
// transaction as the invoice) so the PDF is rendered and stored out of band.
public sealed record InvoiceCreated(InvoiceId InvoiceId, BusinessId BusinessId) : IDomainEvent;
