using Bookkeeping.Application.Common;
using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Invoices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookkeeping.Infrastructure.Documents;

// Drains queued invoice-PDF jobs outside the request path. Each iteration runs in
// its own DI scope (a fresh AppDbContext, no HttpContext), which is what lets it do
// the external I/O — logo fetch and R2 upload — that must not sit inside the invoice
// create transaction. The link is written back through the aggregate (AttachPdf),
// keeping Invoice the single writer of PdfUrl.
public sealed class InvoicePdfOutboxProcessor : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LogoTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InvoicePdfOutboxProcessor> _logger;

    public InvoicePdfOutboxProcessor(
        IServiceScopeFactory scopes,
        IHttpClientFactory httpClientFactory,
        ILogger<InvoicePdfOutboxProcessor> logger)
    {
        _scopes = scopes;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A loop-level failure (e.g. the DB being unreachable) must not kill
                // the service; back off and try again.
                _logger.LogError(ex, "Invoice PDF outbox loop error.");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }

    // Processes one job. Returns true if a job was picked up (success or failure),
    // false if the queue was empty.
    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var sp = scope.ServiceProvider;

        var jobs = sp.GetRequiredService<IInvoicePdfJobRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var job = await jobs.ClaimNextPendingAsync(ct);
        if (job is null)
            return false;

        try
        {
            var detail = await sp.GetRequiredService<IInvoiceService>().GetAsync(job.BusinessId, job.InvoiceId, ct)
                ?? throw new InvalidOperationException($"Invoice {job.InvoiceId.Value} not found.");

            var template = await sp.GetRequiredService<IIdentityService>()
                .GetInvoiceTemplateAsync(job.BusinessId, ct);
            var branding = template.IsSuccess ? template.Value : null;

            var logo = await TryFetchLogoAsync(branding?.LogoUrl, ct);
            var pdf = sp.GetRequiredService<IInvoicePdfGenerator>().Generate(detail, branding, logo);
            var url = await sp.GetRequiredService<IInvoiceDocumentStore>().SaveAsync(job.InvoiceId, pdf, ct);

            // Write-back through the aggregate; the job completion commits with it.
            var repository = sp.GetRequiredService<IInvoiceRepository>();
            var invoice = await repository.GetAsync(job.InvoiceId, ct)
                ?? throw new InvalidOperationException($"Invoice {job.InvoiceId.Value} not found.");
            invoice.AttachPdf(url);
            repository.Update(invoice);

            job.MarkDone();
            await uow.SaveChangesAsync(ct);
            _logger.LogInformation("Generated invoice PDF for {InvoiceId}.", job.InvoiceId.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to generate invoice PDF for {InvoiceId}.", job.InvoiceId.Value);
            job.MarkFailed(ex.Message);
            await uow.SaveChangesAsync(ct);
        }

        return true;
    }

    // Best-effort logo download for embedding. A bad URL, non-image, or network
    // failure yields a logo-less invoice rather than failing the whole job.
    private async Task<byte[]?> TryFetchLogoAsync(string? logoUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = LogoTimeout;

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
}
