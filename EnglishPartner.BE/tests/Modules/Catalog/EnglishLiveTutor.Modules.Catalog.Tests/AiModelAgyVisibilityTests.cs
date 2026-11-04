using EnglishLiveTutor.Application.Features.AiModels.Queries.GetPronunciationModels;
using EnglishLiveTutor.Application.Features.AiModels.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AiModelAgyVisibilityTests
{
    [Fact]
    public async Task LearnerModelQueries_HideAgyCatalogEntries_WhenDisabled()
    {
        await using var db = CreateContext();
        await SeedModelsAsync(db);
        var runner = CreateRunner(enabled: false);

        var pronunciationModels = await new GetPronunciationModelsQueryHandler(db, runner)
            .HandleAsync(new GetPronunciationModelsQuery());
        var vocabularyModels = await new GetVocabularyGenerationModelsQueryHandler(db, runner)
            .HandleAsync(new GetVocabularyGenerationModelsQuery());

        Assert.DoesNotContain(pronunciationModels, model => model.ModelCode == "gemini-3.8-flash-medium");
        Assert.DoesNotContain(vocabularyModels, model => model.ModelCode == "gemini-3.8-flash-medium");
        Assert.Contains(pronunciationModels, model => model.ModelCode == "gemini-3.8-flash");
        Assert.Contains(vocabularyModels, model => model.ModelCode == "gemini-3.8-flash");
    }

    [Fact]
    public async Task LearnerModelQueries_ReturnAgyCatalogEntries_WhenEnabled()
    {
        await using var db = CreateContext();
        await SeedModelsAsync(db);
        var runner = CreateRunner(enabled: true);

        var pronunciationModels = await new GetPronunciationModelsQueryHandler(db, runner)
            .HandleAsync(new GetPronunciationModelsQuery());
        var vocabularyModels = await new GetVocabularyGenerationModelsQueryHandler(db, runner)
            .HandleAsync(new GetVocabularyGenerationModelsQuery());

        Assert.Contains(pronunciationModels, model => model.ModelCode == "gemini-3.8-flash-medium");
        Assert.Contains(vocabularyModels, model => model.ModelCode == "gemini-3.8-flash-medium");
    }

    private static AgyCliRunner CreateRunner(bool enabled) =>
        new(
            Options.Create(new AntigravityCliOptions
            {
                Enabled = enabled,
                AllowedModels = ["gemini-3.8-flash-medium"]
            }),
            NullLogger<AgyCliRunner>.Instance);

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedModelsAsync(AppDbContext db)
    {
        db.AiModelCatalog.AddRange(
            new AiModelConfigEntity(
                "Gemini Cloud",
                "gemini-3.8-flash",
                supportsTextGeneration: true,
                supportsPronunciationAssessment: true),
            new AiModelConfigEntity(
                "AGY Local",
                "gemini-3.8-flash-medium",
                supportsTextGeneration: true,
                supportsPronunciationAssessment: true));
        await db.SaveChangesAsync();
    }
}
