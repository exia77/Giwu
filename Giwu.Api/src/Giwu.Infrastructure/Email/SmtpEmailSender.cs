using Giwu.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Giwu.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> opts, ILogger<SmtpEmailSender> log)
    : IEmailSender
{
    private readonly SmtpOptions _o = opts.Value;

    public async Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_o.Host) || string.IsNullOrWhiteSpace(_o.FromAddress))
        {
            // SMTP not configured — log the body so dev can still test the flow.
            log.LogWarning("SMTP not configured. Skipping send to {To}. Subject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_o.FromName, _o.FromAddress));
        msg.To.Add(new MailboxAddress(toName, toEmail));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var socketOption = _o.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        await client.ConnectAsync(_o.Host, _o.Port, socketOption, ct);

        if (!string.IsNullOrEmpty(_o.Username))
            await client.AuthenticateAsync(_o.Username, _o.Password, ct);

        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
    }
}
