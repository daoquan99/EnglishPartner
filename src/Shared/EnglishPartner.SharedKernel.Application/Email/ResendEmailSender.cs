using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.SharedKernel.Email;

/// <summary>
/// Sends emails via Resend REST API. Use for production.
/// Uses raw HttpClient to avoid complex DI setup of Resend NuGet package.
/// </summary>
public sealed class ResendEmailSender(
    IOptions<EmailSettings> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private static readonly HttpClient Http = new();
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from = $"{_settings.FromName} <{_settings.FromAddress}>",
            to = new[] { message.To },
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.PlainTextBody
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ResendApiKey);

        var response = await Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Email sent via Resend to {To}, subject: {Subject}", message.To, message.Subject);
    }
}
