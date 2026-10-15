namespace EnglishLiveTutor.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "healthy",
            framework = ".NET 10.0",
            architecture = "Pragmatic Clean Architecture + DDD + CQRS",
            database = "SQL Server (Database=LingoMint)",
            supportedModes = 19,
            features = new[]
            {
                "Live AI English Tutor (3 SignalR Hubs)",
                "Daily Vocabulary Flashcards & SM-2 Spaced Repetition",
                "AI Vocabulary Generation (Gemini & Gemma 4 Models)",
                "Pronunciation Assessment (ENGLISH_PRONUNCIATION_V1: Accuracy 40%, Clarity 30%, Fluency 30%)"
            },
            hubs = new[]
            {
                "/hubs/tutor (gemini-3.8-live / gemini-3.8-live-extended-thinking)",
                "/hubs/transcribe (gemini-3.5-transcribe-live)",
                "/hubs/translate (gemini-3.5-live-translate-preview - text only)"
            }
        }));

        return app;
    }
}
