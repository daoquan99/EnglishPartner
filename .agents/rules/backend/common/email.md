---
trigger: manual
---

# Common: Email Service

> **Scope:** Shared email sending — MailKit (SMTP) cho dev, Resend API cho production
> @mention this file when working on: email sending, notification email, welcome email, reset password email

---

## 1. Cấu trúc

```
SharedKernel.Application/Email/
├── IEmailSender.cs              ← Interface duy nhất — tất cả modules inject cái này
├── EmailMessage.cs              ← DTO: To, Subject, HtmlBody, PlainTextBody?
├── EmailSettings.cs             ← Config bind từ appsettings "Email" section
├── SmtpEmailSender.cs           ← MailKit implementation (dev/Mailtrap)
├── ResendEmailSender.cs         ← Raw HttpClient → Resend REST API (production)
└── EmailServiceExtensions.cs    ← DI: services.AddEmailSender(configuration)
```

---

## 2. Interface

```csharp
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
```

---

## 3. Cách dùng trong Handler

```csharp
// Inject IEmailSender vào bất kỳ CommandHandler / IntegrationEventHandler
internal sealed class SomeHandler(IEmailSender emailSender) : ICommandHandler<SomeCommand>
{
    public async Task<Result> Handle(SomeCommand request, CancellationToken ct)
    {
        // Plain string interpolation — không dùng template engine
        var html = $"""
            <h1>Hello {request.UserName}!</h1>
            <p>Welcome to EnglishPartner. Start learning today.</p>
            """;

        await emailSender.SendAsync(new EmailMessage(
            To: request.Email,
            Subject: "Welcome to EnglishPartner!",
            HtmlBody: html
        ), ct);

        return Result.Success();
    }
}
```

---

## 4. Config

### appsettings.Development.json (SMTP / Mailtrap)
```json
{
  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@englishpartner.dev",
    "FromName": "EnglishPartner (Dev)",
    "SmtpHost": "sandbox.smtp.mailtrap.io",
    "SmtpPort": 587,
    "SmtpUsername": "YOUR_MAILTRAP_USER",
    "SmtpPassword": "YOUR_MAILTRAP_PASS",
    "SmtpUseSsl": true
  }
}
```

### appsettings.Production.json (Resend)
```json
{
  "Email": {
    "Provider": "Resend",
    "FromAddress": "noreply@englishpartner.app",
    "FromName": "EnglishPartner",
    "ResendApiKey": "re_xxxxxxxxxxxx"
  }
}
```

---

## 5. Provider switching

`EmailServiceExtensions.AddEmailSender()` đọc `Email:Provider`:
- `"Smtp"` → register `SmtpEmailSender` (MailKit)
- `"Resend"` → register `ResendEmailSender` (raw HttpClient)

Swap bằng config, zero code change.

---

## 6. DI Registration (Program.cs)

```csharp
using EnglishPartner.SharedKernel.Email;

builder.Services.AddEmailSender(builder.Configuration);
```

Đã được đăng ký sẵn. Không cần register lại trong từng module.

---

## 7. Quy tắc

1. **Nội dung email:** Dùng plain string interpolation (`$"<h1>..."`) — KHÔNG dùng Razor template hay template engine.
2. **Ai gọi:** Module nào cần gửi email thì inject `IEmailSender` — Identity (welcome, reset password), Notification (daily reminder fallback), Admin (broadcast).
3. **Không gửi email trực tiếp từ API endpoint.** Luôn gửi từ CommandHandler hoặc IntegrationEventHandler.
4. **Async only.** Luôn dùng `SendAsync` với `CancellationToken`.
5. **Không hardcode email address.** Lấy `FromAddress` / `FromName` từ `EmailSettings`.

---

## 8. Use cases dự kiến

| Module | Use case | Trigger |
|---|---|---|
| Identity | Welcome email | Sau `UserRegisteredDomainEvent` |
| Identity | Reset password | Sau `POST /auth/forgot-password` |
| Notification | Daily reminder (fallback) | `DailyGoalReminderIntegrationEventHandler` khi user offline |
| Admin | Broadcast announcement | Admin API |
