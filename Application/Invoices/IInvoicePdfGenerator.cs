using Bookkeeping.Application.Identity;

namespace Bookkeeping.Application.Invoices;

// Renders an invoice to a print-ready PDF. The implementation lives in
// Infrastructure so the application layer stays free of the PDF library.
// The template (business branding, bank details, terms) is optional: a business
// that has not configured one still gets a valid, if plainer, document.
// The logo is passed in as already-downloaded bytes so the generator stays pure
// (no network I/O); a null logo simply falls back to the business name.
public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceDetail invoice, InvoiceTemplateDto? template, byte[]? logo = null);
}
