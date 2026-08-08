using Bookkeeping.Application.Ledger;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.Ledger.Read)]
[Route("api/businesses/{businessId:guid}")]
public sealed class LedgerController(ILedgerService ledger) : ControllerBase
{
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance(Guid businessId, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await ledger.GetTrialBalanceAsync(new BusinessId(businessId), date, ct));
    }
}
