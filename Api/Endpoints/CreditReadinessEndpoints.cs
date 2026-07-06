using Bookkeeping.Application.CreditReadiness;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Api.Endpoints;

public static class CreditReadinessEndpoints
{
    public static IEndpointRouteBuilder MapCreditReadinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/businesses/{businessId:guid}/credit-readiness/sufficiency",
            async (Guid businessId, DateOnly from, DateOnly to, ICreditReadinessService credit, CancellationToken ct) =>
                Results.Ok(await credit.CheckDataSufficiencyAsync(new BusinessId(businessId), new DateRange(from, to), ct)));

        app.MapGet("/api/businesses/{businessId:guid}/credit-readiness",
            async (Guid businessId, DateOnly from, DateOnly to, ICreditReadinessService credit, CancellationToken ct) =>
            {
                var result = await credit.GenerateReportAsync(new BusinessId(businessId), new DateRange(from, to), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(new { error = result.Error });
            });

        return app;
    }
}
