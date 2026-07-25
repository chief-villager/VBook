using Bookkeeping.Application.CreditReadiness;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/credit-readiness")]
public sealed class CreditReadinessController(ICreditReadinessService credit) : ControllerBase
{
    [HttpGet("sufficiency")]
    public async Task<IActionResult> Sufficiency(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await credit.CheckDataSufficiencyAsync(new BusinessId(businessId), new DateRange(from, to), ct));

    [HttpGet]
    public async Task<IActionResult> Report(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await credit.GenerateReportAsync(new BusinessId(businessId), new DateRange(from, to), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
