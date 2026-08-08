using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Bookkeeping.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Infrastructure.Documents;

// Cloudflare R2 is S3-compatible, so the AWS S3 SDK talks to it once pointed at the
// R2 endpoint. The client is thread-safe and meant to be reused, so this store is a
// singleton. This is the general-purpose object store; module-specific stores (e.g.
// invoice PDFs) build their keys and delegate here.
public sealed class R2ObjectStore : IObjectStore
{
    private readonly R2Options _options;
    private readonly IAmazonS3 _client;

    public R2ObjectStore(IOptions<R2Options> options)
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
                // AWSSDK.S3 v4 defaults to flexible checksums, which stream the body with a
                // trailing CRC32 (STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER). R2 doesn't
                // implement that trailer, so only compute/validate checksums when an
                // operation actually requires it (none of ours do).
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public async Task<string> PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // The caller owns the stream; don't let the SDK close it out from under them.
            AutoCloseStream = false,
            // AWSSDK.S3 v4 signs the body in chunks as it streams
            // (STREAMING-AWS4-HMAC-SHA256-PAYLOAD), which R2 doesn't support. Send an
            // unsigned payload instead; the request itself is still SigV4-signed and the
            // connection is HTTPS, so it stays secure.
            DisablePayloadSigning = true,
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
