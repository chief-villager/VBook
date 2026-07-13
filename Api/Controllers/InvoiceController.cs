using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/invoices")]
public sealed class InvoiceController(IInvoiceService invoices) : ControllerBase
{
    public sealed record CreateInvoiceRequest(
        string InvoiceNumber,
        DateOnly IssueDate,
        DateOnly DueDate,
        string BillTo,
        decimal VatRate,
        IReadOnlyList<InvoiceLineItemDto> LineItems);

    [HttpPost]
    public async Task<IActionResult> Create(Guid businessId, CreateInvoiceRequest body, CancellationToken ct)
    {
        var command = new CreateInvoiceCommand(
            new BusinessId(businessId), body.InvoiceNumber, body.IssueDate,
            body.DueDate, body.BillTo, body.VatRate, body.LineItems);
        var result = await invoices.CreateAsync(command, ct);
        return result.IsSuccess
            ? Ok(new { invoiceId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid businessId, CancellationToken ct)
        => Ok(await invoices.ListAsync(new BusinessId(businessId), ct));

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> Get(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await invoices.GetAsync(new InvoiceId(invoiceId), ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("{invoiceId:guid}/paid")]
    public async Task<IActionResult> MarkAsPaid(Guid invoiceId, CancellationToken ct)
    {
        var result = await invoices.MarkAsPaidAsync(new InvoiceId(invoiceId), ct);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }
}
