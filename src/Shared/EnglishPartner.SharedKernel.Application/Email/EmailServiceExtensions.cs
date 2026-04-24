using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.SharedKernel.Email;

public static class EmailServiceExtensions
{
    /// <summary>
    /// Registers IEmailSender with the appropriate provider based on config.
    /// Config section: "Email" → Provider = "Smtp" | "Resend"
    /// </summary>
    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        var provider = configuration.GetValue<string>("Email:Provider") ?? "Smtp";

        if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailSender, ResendEmailSender>();
        else
            services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
