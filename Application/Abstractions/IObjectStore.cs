namespace Bookkeeping.Application.Abstractions;

// Provider-neutral object storage port. The application layer knows only the
// contract; the implementation (Cloudflare R2 over the S3 API) lives in
// Infrastructure. General-purpose so any module can store binary objects
// (invoice PDFs, business logos, …) without depending on a vendor SDK.
public interface IObjectStore
{
    // Stores the content under the given key and returns a URL to retrieve it
    // (a public URL when a public base is configured, otherwise a presigned one).
    // The caller owns the stream and is responsible for disposing it.
    Task<string> PutAsync(string key, Stream content, string contentType, CancellationToken ct = default);
}
