using Bookkeeping.Api.RateLimiting;
using Bookkeeping.Application.ChatBot;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookkeeping.Api.Controllers;

// The assistant reads both transactions and statements, so the caller needs both
// permissions on the route's business (stacked [Authorize] policies must all pass).
[ApiController]
[Authorize(Policy = Permissions.Reports.Read)]
[Authorize(Policy = Permissions.Transactions.Read)]
[EnableRateLimiting(RateLimitingExtensions.ChatPolicy)]
[Route("api/businesses/{businessId:guid}/chat")]
public sealed class ChatBotController(IChatBotService chatBot, ILogger<ChatBotController> logger) : ControllerBase
{
    public sealed record AskRequest(string Question);

    [HttpPost]
    public async Task<IActionResult> Ask(Guid businessId, AskRequest body, CancellationToken ct)
    {
        try
        {
            var result = await chatBot.AskAsync(new BusinessId(businessId), body.Question, ct);
            return result.IsSuccess
                ? Ok(new { answer = result.Value })
                : BadRequest(new { error = result.Error });
        }
        catch (HttpRequestException ex)
        {
            // The model server (Ollama) is down or unreachable.
            logger.LogError(ex, "Chat model request failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "The assistant is unavailable right now. Please try again later." });
        }
    }
}
