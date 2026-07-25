using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bookkeeping.Infrastructure.Mono;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Api.Controllers;

// Receives asynchronous notifications from Mono (account updates, reauth prompts,
// unlink confirmations, etc.). Mono authenticates the call by echoing our configured
// secret in the "mono-webhook-secret" header — there is no HMAC signature, so we
// compare the header to MonoOptions.WebhookSecret before trusting the body.
[ApiController]
[Route("api/webhooks/mono")]
public sealed class MonoWebhookController(
    IOptions<MonoOptions> options,
    ILogger<MonoWebhookController> logger) : ControllerBase
{
    private readonly MonoOptions _mono = options.Value;

    // The payload shape is stable at the envelope level; the "data" varies per event,
    // so we keep it as a raw element and only inspect it for events we act on.
    public sealed record WebhookEnvelope(
        [property: JsonPropertyName("event")] string Event,
        [property: JsonPropertyName("data")] JsonElement Data);

    [HttpPost]
    public IActionResult Receive(
        [FromHeader(Name = "mono-webhook-secret")] string? secret,
        [FromBody] WebhookEnvelope payload)
    {
        if (!IsSecretValid(secret))
        {
            logger.LogWarning("Rejected Mono webhook with missing or invalid secret header.");
            return Unauthorized();
        }

        // Acknowledge fast; Mono retries on non-2xx. Real side effects belong on a
        // domain-event/handler seam rather than inline here.
        switch (payload.Event)
        {
            case "mono.events.account_connected":
            case "mono.events.account_updated":
                logger.LogInformation("Mono account event {Event} received.", payload.Event);
                break;

            case "mono.events.account_unlinked":
                logger.LogInformation("Mono account unlinked.");
                break;

            case "mono.events.reauthorisation_required":
                logger.LogWarning("Mono account requires reauthorisation.");
                break;

            default:
                logger.LogInformation("Unhandled Mono webhook event {Event}.", payload.Event);
                break;
        }

        return Ok();
    }

    // Constant-time comparison so a caller can't probe the secret by timing responses.
    private bool IsSecretValid(string? provided)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(_mono.WebhookSecret))
            return false;

        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(_mono.WebhookSecret);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
