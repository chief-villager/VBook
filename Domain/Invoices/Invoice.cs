using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Invoices;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public BusinessId BusinessId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string BillTo { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; }
    public decimal VatRate { get; private set; } = 0.075m;

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    // Money is derived from the lines, never stored, so it can never drift from them.
    public decimal Subtotal => _lineItems.Sum(i => i.LineTotal);
    public decimal VatAmount => decimal.Round(Subtotal * VatRate, 2);
    public decimal TotalAmount => Subtotal + VatAmount;

    private Invoice() { }

    private Invoice(BusinessId businessId, string invoiceNumber, DateOnly issueDate,
        DateOnly dueDate, string billTo, decimal vatRate, IEnumerable<InvoiceLineItem> lineItems)
    {
        Id = InvoiceId.New();
        BusinessId = businessId;
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        BillTo = billTo;
        VatRate = vatRate;
        Status = InvoiceStatus.Pending;
        _lineItems.AddRange(lineItems);
    }

    public static Result<Invoice> Create(BusinessId businessId, string invoiceNumber, DateOnly issueDate,
        DateOnly dueDate, string billTo, decimal vatRate, IEnumerable<InvoiceLineItem> lineItems)
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

        return new Invoice(businessId, invoiceNumber.Trim(), issueDate, dueDate, billTo.Trim(), vatRate, items);
    }

    public Result MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            return Result.Failure("Invoice is already paid.");

        Status = InvoiceStatus.Paid;
        return Result.Success();
    }
}
