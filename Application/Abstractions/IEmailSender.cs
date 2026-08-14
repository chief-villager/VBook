namespace Bookkeeping.Application.Abstractions;

// Provider-neutral outbound email port. The application layer knows only this
// contract; the transport (SMTP) lives in Infrastructure so vendor/protocol types
// never leak upward. Callers pass an already-rendered subject and HTML body.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
