namespace Bookkeeping.Infrastructure.Documents;

// Cloudflare R2 connection settings, bound from the "R2" configuration section.
public sealed class R2Options
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;

    // Public base URL for retrieval (an r2.dev domain or a custom domain bound to the
    // bucket). When empty, a time-limited presigned GET URL is generated instead.
    public string PublicBaseUrl { get; set; } = string.Empty;
}
