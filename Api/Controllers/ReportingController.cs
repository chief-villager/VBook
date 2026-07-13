using Bookkeeping.Application.Reporting;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/reports")]
public sealed class ReportingController(IFinancialReportingService reporting) : ControllerBase
{
    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await reporting.ProfitAndLossAsync(new BusinessId(businessId), new DateRange(from, to), ct));

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(Guid businessId, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await reporting.BalanceSheetAsync(new BusinessId(businessId), date, ct));
    }

    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await reporting.CashFlowAsync(new BusinessId(businessId), new DateRange(from, to), ct));
}
