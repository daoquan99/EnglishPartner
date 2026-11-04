using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Hosting;

namespace EnglishLiveTutor.Api.Configuration;

internal static class AntigravityCliStartupValidator
{
    public static void Validate(IHostEnvironment environment, AntigravityCliOptions options)
    {
        if (options.Enabled && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Ai:AntigravityCli:Enabled is only allowed in the Development environment.");
        }
    }
}
