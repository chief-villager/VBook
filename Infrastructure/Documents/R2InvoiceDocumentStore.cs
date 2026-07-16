using Amazon.S3;
using Amazon.S3.Model;
using Bookkeeping.Application.Invoices;
using Bookkeeping.Domain.Common;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Infrastructure.Documents;

// Cloudflare R2 is S3-compatible, so the AWS S3 SDK talks to it once pointed at the
// R2 endpoint. The client is thread-safe and meant to be reused, so this store is a
// singleton.
public sealed class R2InvoiceDocumentStore : IInvoiceDocumentStore
{
    private readonly R2Options _options;
    private readonly IAmazonS3 _client;

    public R2InvoiceDocumentStore(IOptions<R2Options> options)
    {
        _options = options.Value;
        _client = new AmazonS3Client(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                // R2 addresses buckets by path, not by subdomain.
                ForcePathStyle = true,
            });
    }

    public async Task<string> SaveAsync(InvoiceId invoiceId, byte[] pdf, CancellationToken ct = default)
    {
        var key = $"invoices/{invoiceId.Value}.pdf";

        using var stream = new MemoryStream(pdf);
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = "application/pdf",
        }, ct);

        // A configured public domain is returned directly; otherwise hand back a
        // presigned link that is valid for a week.
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

        return await _client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddDays(7),
        });
    }
}
