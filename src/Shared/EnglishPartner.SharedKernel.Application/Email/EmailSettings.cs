namespace EnglishPartner.SharedKernel.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>"Smtp" or "Resend"</summary>
    public string Provider { get; set; } = "Smtp";

    public string FromAddress { get; set; } = "noreply@englishpartner.app";
    public string FromName { get; set; } = "EnglishPartner";

    // ── SMTP (MailKit) ──
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;

    // ── Resend ──
    public string? ResendApiKey { get; set; }
}
