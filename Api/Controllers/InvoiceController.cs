using Bookkeeping.Application.Common;
using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/invoices")]
public sealed class InvoiceController(
    IInvoiceService invoices,
    IIdentityService identity,
    IInvoicePdfGenerator pdfGenerator,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    public sealed record CreateInvoiceRequest(
        string InvoiceNumber,
        DateOnly IssueDate,
        DateOnly DueDate,
        string BillTo,
        decimal VatRate,
        IReadOnlyList<InvoiceLineItemDto> LineItems);

    [Authorize(Policy = Permissions.Invoices.Create)]
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

    [Authorize(Policy = Permissions.Invoices.Read)]
    [HttpGet]
    public async Task<IActionResult> List(
        Guid businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await invoices.ListAsync(new BusinessId(businessId), new PageRequest(page, pageSize), ct));

    [Authorize(Policy = Permissions.Invoices.Read)]
    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> Get(Guid businessId, Guid invoiceId, CancellationToken ct)
    {
        var invoice = await invoices.GetAsync(new BusinessId(businessId), new InvoiceId(invoiceId), ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [Authorize(Policy = Permissions.Invoices.Read)]
    [HttpGet("{invoiceId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid businessId, Guid invoiceId, CancellationToken ct)
    {
        var invoice = await invoices.GetAsync(new BusinessId(businessId), new InvoiceId(invoiceId), ct);
        if (invoice is null)
            return NotFound();

        // Branding is best-effort: an invoice still renders without a template.
        var template = await identity.GetInvoiceTemplateAsync(new BusinessId(businessId), ct);
        var branding = template.IsSuccess ? template.Value : null;

        var logo = await TryFetchLogoAsync(branding?.LogoUrl, ct);
        var pdf = pdfGenerator.Generate(invoice, branding, logo);

        return File(pdf, "application/pdf", $"invoice-{invoice.InvoiceNumber}.pdf");
    }

    // Downloads the logo for embedding. Best-effort: a bad URL, a non-image, or a
    // network failure just yields a logo-less invoice rather than a failed request.
    private async Task<byte[]?> TryFetchLogoAsync(string? logoUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var response = await client.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") != true)
                return null;

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    [Authorize(Policy = Permissions.Invoices.Update)]
    [HttpPost("{invoiceId:guid}/paid")]
    public async Task<IActionResult> MarkAsPaid(Guid businessId, Guid invoiceId, CancellationToken ct)
    {
        var result = await invoices.MarkAsPaidAsync(new BusinessId(businessId), new InvoiceId(invoiceId), ct);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }
}
