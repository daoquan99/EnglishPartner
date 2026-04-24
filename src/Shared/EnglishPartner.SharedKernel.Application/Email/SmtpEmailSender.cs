using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EnglishPartner.SharedKernel.Email;

/// <summary>
/// Sends emails via SMTP using MailKit. Use for development (Mailtrap, Gmail SMTP, etc).
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        mime.Body = body.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            _settings.SmtpUseSsl
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrEmpty(_settings.SmtpUsername))
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email sent via SMTP to {To}, subject: {Subject}", message.To, message.Subject);
    }
}
