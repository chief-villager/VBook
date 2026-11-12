namespace Bookkeeping.Api.RateLimiting;

public static class RateLimitingExtensions
{
    // Policy names, referenced from [EnableRateLimiting(...)] on controllers/actions.
    public const string TransactionPolicy = "TransactionRateLimit";
    public const string LoginPolicy = "LoginRateLimit";
    public const string ChatPolicy = "ChatRateLimit";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(TransactionPolicy, httpContext =>
                EndpointLimiter.PerUserPerEndpoint(httpContext, permitLimit: 5, window: TimeSpan.FromHours(1)));
            options.AddPolicy(LoginPolicy, httpContext =>
                EndpointLimiter.PerUserPerEndpoint(httpContext, permitLimit: 5, window: TimeSpan.FromMinutes(1)));
            // Each chat question runs a local LLM with tool calls — expensive, so cap it.
            options.AddPolicy(ChatPolicy, httpContext =>
                EndpointLimiter.PerUserPerEndpoint(httpContext, permitLimit: 20, window: TimeSpan.FromMinutes(1)));
        });

        return services;
    }
}
