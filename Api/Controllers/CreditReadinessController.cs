using Bookkeeping.Application.CreditReadiness;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.CreditReadiness.Read)]
[Route("api/businesses/{businessId:guid}/credit-readiness")]
public sealed class CreditReadinessController(ICreditReadinessService credit) : ControllerBase
{   
    [Authorize(Policy = Permissions.CreditReadiness.Read)]
    [HttpGet("sufficiency")]
    public async Task<IActionResult> Sufficiency(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await credit.CheckDataSufficiencyAsync(new BusinessId(businessId), new DateRange(from, to), ct));

    [Authorize(Policy = Permissions.CreditReadiness.Read)]
    [HttpGet]
    public async Task<IActionResult> Report(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await credit.GenerateReportAsync(new BusinessId(businessId), new DateRange(from, to), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.CreditReadiness.Read)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await credit.CreditReadinessDashBoard(new BusinessId(businessId), new DateRange(from, to), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.CreditReadiness.Read)]
    [HttpGet("evaluation")]
    public async Task<IActionResult> Evaluation(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await credit.EvaluateCreditReadinessAsync(new BusinessId(businessId), new DateRange(from, to), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
