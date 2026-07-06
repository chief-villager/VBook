using Bookkeeping.Application.Reporting;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/businesses/{businessId:guid}/reports/profit-and-loss",
            async (Guid businessId, DateOnly from, DateOnly to, IFinancialReportingService reporting, CancellationToken ct) =>
                Results.Ok(await reporting.ProfitAndLossAsync(new BusinessId(businessId), new DateRange(from, to), ct)));

        app.MapGet("/api/businesses/{businessId:guid}/reports/balance-sheet",
            async (Guid businessId, DateOnly? asOf, IFinancialReportingService reporting, CancellationToken ct) =>
            {
                var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
                return Results.Ok(await reporting.BalanceSheetAsync(new BusinessId(businessId), date, ct));
            });

        app.MapGet("/api/businesses/{businessId:guid}/reports/cash-flow",
            async (Guid businessId, DateOnly from, DateOnly to, IFinancialReportingService reporting, CancellationToken ct) =>
                Results.Ok(await reporting.CashFlowAsync(new BusinessId(businessId), new DateRange(from, to), ct)));

        return app;
    }
}
