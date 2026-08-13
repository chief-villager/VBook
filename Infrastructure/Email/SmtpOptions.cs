using System.ComponentModel.DataAnnotations;

namespace Bookkeeping.Infrastructure.Email;

// Bound from the "Smtp" config section and validated at startup. For local dev,
// point Host/Port at a fake inbox (e.g. Mailhog/Papercut on 1025) with no auth.
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required] public string Host { get; init; } = string.Empty;
    [Range(1, 65535)] public int Port { get; init; } = 25;

    // Optional: leave empty for an anonymous relay (local fake inboxes).
    public string? User { get; init; }
    public string? Password { get; init; }

    [Required, EmailAddress] public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;

    public bool UseSsl { get; init; }
}
