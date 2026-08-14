using Bookkeeping.Application.Common;
using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}")]
public sealed class TransactionsController(ITransactionService transactions) : ControllerBase
{
    public sealed record RecordTransactionRequest(decimal Amount, Guid CategoryId, DateOnly OccurredOn, string? Note);

    [Authorize(Policy = Permissions.Transactions.Read)]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(Guid businessId, CancellationToken ct)
        => Ok(await transactions.GetCategoriesAsync(new BusinessId(businessId), ct));

    [Authorize(Policy = Permissions.Transactions.Record)]
    [HttpPost("transactions")]
    public async Task<IActionResult> Record(Guid businessId, RecordTransactionRequest body, CancellationToken ct)
    {
        var command = new RecordTransactionCommand(
            new BusinessId(businessId), body.Amount, new CategoryId(body.CategoryId), body.OccurredOn, body.Note);
        var result = await transactions.RecordAsync(command, ct);
        return result.IsSuccess
            ? Ok(new { transactionId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.Transactions.Read)]
    [HttpGet("transactions")]
    public async Task<IActionResult> List(
        Guid businessId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await transactions.ListPagedAsync(
            new BusinessId(businessId), new DateRange(from, to), new PageRequest(page, pageSize), ct));
}
