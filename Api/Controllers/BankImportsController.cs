using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

// The bank-import review queue. Pulling stages a linked account's feed as pending
// rows; nothing reaches the ledger until a row is categorised and approved.
[ApiController]
[Route("api/businesses/{businessId:guid}/bank-imports")]
public sealed class BankImportsController(IBankImportService imports) : ControllerBase
{
    public sealed record PullRequest(string ExternalAccountId, DateOnly From, DateOnly To);
    public sealed record CategoriseRequest(Guid CategoryId);

    [Authorize(Policy = Permissions.BankImports.Manage)]
    [HttpPost]
    public async Task<IActionResult> Pull(Guid businessId, PullRequest body, CancellationToken ct)
    {
        var command = new PullBankFeedCommand(
            new BusinessId(businessId), body.ExternalAccountId, new DateRange(body.From, body.To));

        var result = await imports.PullAsync(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.BankImports.Read)]
    [HttpGet]
    public async Task<IActionResult> List(Guid businessId, [FromQuery] StagedTransactionStatus status = StagedTransactionStatus.Pending, CancellationToken ct = default)
        => Ok(await imports.ListAsync(new BusinessId(businessId), status, ct));

    [Authorize(Policy = Permissions.BankImports.Manage)]
    [HttpPatch("{stagedId:guid}")]
    public async Task<IActionResult> Categorise(Guid stagedId, CategoriseRequest body, CancellationToken ct)
    {
        var result = await imports.CategoriseAsync(
            new StagedTransactionId(stagedId), new CategoryId(body.CategoryId), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.BankImports.Manage)]
    [HttpPost("{stagedId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid stagedId, CancellationToken ct)
    {
        var result = await imports.ApproveAsync(new StagedTransactionId(stagedId), ct);
        return result.IsSuccess
            ? Ok(new { transactionId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.BankImports.Manage)]
    [HttpPost("{stagedId:guid}/discard")]
    public async Task<IActionResult> Discard(Guid stagedId, CancellationToken ct)
    {
        var result = await imports.DiscardAsync(new StagedTransactionId(stagedId), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
