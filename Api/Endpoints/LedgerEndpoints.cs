using Bookkeeping.Application.Ledger;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/businesses/{businessId:guid}/trial-balance",
            async (Guid businessId, DateOnly? asOf, ILedgerService ledger, CancellationToken ct) =>
            {
                var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
                return Results.Ok(await ledger.GetTrialBalanceAsync(new BusinessId(businessId), date, ct));
            });

        return app;
    }
}
