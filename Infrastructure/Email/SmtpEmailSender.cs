using System.Net;
using System.Net.Mail;
using Bookkeeping.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookkeeping.Infrastructure.Email;

// SMTP adapter for IEmailSender using System.Net.Mail. A fresh SmtpClient/MailMessage
// per send keeps this stateless; authentication is used only when a user is configured
// (anonymous otherwise, for local fake inboxes).
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options) => _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
        if (!string.IsNullOrEmpty(_options.User))
            client.Credentials = new NetworkCredential(_options.User, _options.Password);

        await client.SendMailAsync(message, ct);
    }
}
