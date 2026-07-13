using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public sealed record CreateInvoiceCommand(
    BusinessId BusinessId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string BillTo,
    decimal VatRate,
    IReadOnlyList<InvoiceLineItemDto> LineItems);

public sealed record InvoiceLineItemDto(string Description, decimal Quantity, decimal UnitPrice);

// Lightweight row for listings.
public sealed record InvoiceSummary(
    InvoiceId Id,
    string InvoiceNumber,
    string BillTo,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    decimal TotalAmount);

// Full view of one invoice, including the derived money breakdown and its lines.
public sealed record InvoiceDetail(
    InvoiceId Id,
    BusinessId BusinessId,
    string InvoiceNumber,
    string BillTo,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal TotalAmount,
    IReadOnlyList<InvoiceLineItemDto> LineItems);
