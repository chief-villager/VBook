using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Bookkeeping.Api.RateLimiting;

// Builds rate-limit partitions keyed per caller + endpoint. The HttpContext is the one
// the rate-limiter middleware hands to the policy callback for the current request; it
// is only read synchronously to build the key and never stored.
public static class EndpointLimiter
{
    public static RateLimitPartition<string> PerUserPerEndpoint(HttpContext httpContext, int permitLimit, TimeSpan window)
    {
        // "sub" maps to NameIdentifier under the default inbound claim mapping; fall back
        // to the raw claim, then to the client IP for anonymous callers.
        var identity = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        // Route template (not the concrete path) so /businesses/{id}/... shares one bucket
        // per user rather than one per business id.
        var route = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
            ?? httpContext.Request.Path.Value
            ?? "unknown";

        var partitionKey = $"{identity}:{httpContext.Request.Method}:{route}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            // Reject immediately with 429 rather than holding requests open until the
            // window resets (which could be up to the full window length).
            QueueLimit = 0
        });
    }
}
