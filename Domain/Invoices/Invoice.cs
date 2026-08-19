using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices.Events;

namespace Bookkeeping.Domain.Invoices;

public sealed class Invoice : AggregateRoot<InvoiceId>, IBusinessScoped
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public BusinessId BusinessId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string BillTo { get; private set; } = string.Empty;
    public string? CustomerEmail { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? Note { get; private set; }
    public decimal VatRate { get; private set; } = 0.075m;

    // Set once the PDF has been rendered and stored; null until then.
    public string? PdfUrl { get; private set; }

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    // Money is derived from the lines, never stored, so it can never drift from them.
    public decimal Subtotal => _lineItems.Sum(i => i.LineTotal);
    public decimal VatAmount => decimal.Round(Subtotal * VatRate, 2);
    public decimal TotalAmount => Subtotal + VatAmount;

    private Invoice() { }

    private Invoice(BusinessId businessId, string invoiceNumber, DateOnly issueDate,
        DateOnly dueDate, string billTo, string? customerEmail, string? note, decimal vatRate, IEnumerable<InvoiceLineItem> lineItems)
    {
        Id = InvoiceId.New();
        BusinessId = businessId;
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        BillTo = billTo;
        CustomerEmail = customerEmail;
        Note = note;
        VatRate = vatRate;
        Status = InvoiceStatus.Pending;
        _lineItems.AddRange(lineItems);

        // Kicks off out-of-band PDF generation via the outbox (see InvoiceCreated).
        Raise(new InvoiceCreated(Id, businessId));
    }

    // The single write path for the stored PDF link; called by the outbox processor
    // once the document is generated and uploaded.
    public void AttachPdf(string url) => PdfUrl = url;

    public static Result<Invoice> Create(BusinessId businessId, string invoiceNumber, DateOnly issueDate,
        DateOnly dueDate, string billTo, string? customerEmail, string? note, decimal vatRate, IEnumerable<InvoiceLineItem> lineItems)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return Result<Invoice>.Failure("Invoice number is required.");
        if (string.IsNullOrWhiteSpace(billTo))
            return Result<Invoice>.Failure("Bill-to is required.");
        if (dueDate < issueDate)
            return Result<Invoice>.Failure("Due date cannot be before the issue date.");
        if (vatRate < 0)
            return Result<Invoice>.Failure("VAT rate cannot be negative.");

        var items = lineItems?.ToList() ?? new List<InvoiceLineItem>();
        if (items.Count == 0)
            return Result<Invoice>.Failure("An invoice needs at least one line item.");
        if (items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
            return Result<Invoice>.Failure("Line items need a positive quantity and a non-negative price.");

        // Customer email is optional; normalise a blank to null so it doesn't persist as "".
        var email = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim();

        return new Invoice(businessId, invoiceNumber.Trim(), issueDate, dueDate, billTo.Trim(), email, note, vatRate, items);
    }

    public Result MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            return Result.Failure("Invoice is already paid.");

        Status = InvoiceStatus.Paid;
        return Result.Success();
    }
}
