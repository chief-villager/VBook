using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}")]
public sealed class TransactionsController(ITransactionService transactions) : ControllerBase
{
    public sealed record RecordTransactionRequest(decimal Amount, Guid CategoryId, DateOnly OccurredOn, string? Note);

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(Guid businessId, CancellationToken ct)
        => Ok(await transactions.GetCategoriesAsync(new BusinessId(businessId), ct));

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

    [HttpGet("transactions")]
    public async Task<IActionResult> List(Guid businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await transactions.ListAsync(new BusinessId(businessId), new DateRange(from, to), ct));
}
