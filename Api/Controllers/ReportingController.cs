using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Reporting;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.Reports.Read)]
[Route("api/businesses/{businessId:guid}/reports")]
public sealed class ReportingController(
    IFinancialReportingService reporting,
    IFinancialReportPdfGenerator pdf,
    IIdentityService identity) : ControllerBase
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

    // Computes the requested statement and streams it back as a PDF. `type` selects
    // the report (ProfitAndLoss / BalanceSheet / CashFlow, bound case-insensitively by
    // the enum member name); the period reports need `from` and `to`, the balance
    // sheet an optional `asOf` (defaults to today).
    [HttpGet("download")]
    public async Task<IActionResult> Download(
        Guid businessId,
        [FromQuery] ReportType type,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] DateOnly? asOf,
        CancellationToken ct)
    {
        var id = new BusinessId(businessId);

        var business = await identity.GetBusinessAsync(id, ct);
        if (business.IsFailure)
            return NotFound(new { error = business.Error });
        var name = business.Value.Name;

        switch (type)
        {
            case ReportType.ProfitAndLoss:
            {
                if (from is null || to is null)
                    return BadRequest(new { error = "from and to are required for a profit & loss report." });
                var report = await reporting.ProfitAndLossAsync(id, new DateRange(from.Value, to.Value), ct);
                return Pdf(pdf.ProfitAndLoss(report, name), $"profit-and-loss-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}");
            }
            case ReportType.BalanceSheet:
            {
                var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var report = await reporting.BalanceSheetAsync(id, date, ct);
                return Pdf(pdf.BalanceSheet(report, name), $"balance-sheet-{date:yyyy-MM-dd}");
            }
            case ReportType.CashFlow:
            {
                if (from is null || to is null)
                    return BadRequest(new { error = "from and to are required for a cash flow report." });
                var report = await reporting.CashFlowAsync(id, new DateRange(from.Value, to.Value), ct);
                return Pdf(pdf.CashFlow(report, name), $"cash-flow-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}");
            }
            default:
                return BadRequest(new { error = "Unknown report type." });
        }
    }

    private FileContentResult Pdf(byte[] bytes, string fileName)
        => File(bytes, "application/pdf", $"{fileName}.pdf");
}
