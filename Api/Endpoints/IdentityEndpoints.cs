using Bookkeeping.Application.Identity;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Api.Endpoints;

public static class IdentityEndpoints
{
    public sealed record RegisterUserRequest(string Email, string DisplayName, string Password);
    public sealed record RegisterBusinessRequest(Guid OwnerId, string Name, BusinessSector Sector);
    public sealed record LoginRequest(string Email, string Password);

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", async (RegisterUserRequest body, IIdentityService identity, CancellationToken ct) =>
        {
            var result = await identity.RegisterUserAsync(body.Email, body.DisplayName, body.Password, ct);
            return result.IsSuccess
                ? Results.Ok(new { userId = result.Value.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        app.MapPost("/api/auth/login", async (LoginRequest body, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.SignInAsync(body.Email, body.Password, ct);
            return result.IsSuccess
                ? Results.Ok(new { token = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        app.MapPost("/api/businesses", async (RegisterBusinessRequest body, IIdentityService identity, CancellationToken ct) =>
        {
            var result = await identity.RegisterBusinessAsync(new UserId(body.OwnerId), body.Name, body.Sector, ct);
            return result.IsSuccess
                ? Results.Ok(new { businessId = result.Value.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        app.MapGet("/api/businesses/{businessId:guid}", async (Guid businessId, IIdentityService identity, CancellationToken ct) =>
        {
            var result = await identity.GetBusinessAsync(new BusinessId(businessId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        return app;
    }
}
