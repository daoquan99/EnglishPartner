using EnglishLiveTutor.Application.Features.AiModels.Services;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AiModelCatalogReaderTests
{
    [Fact]
    public async Task FindByModelCodeAsync_ExistingModel_ReturnsCallerMembers()
    {
        await using var db = CreateContext();
        var entity = new AiModelConfigEntity(
            "Gemini Audio",
            "gemini-audio",
            supportsTextGeneration: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsLiveAudio: true,
            isAvailable: true,
            isEnabled: true);
        db.AiModelCatalog.Add(entity);
        await db.SaveChangesAsync();

        var result = await new AiModelCatalogReader(db).FindByModelCodeAsync("gemini-audio");

        Assert.NotNull(result);
        Assert.Equal("Gemini Audio", result.DisplayName);
        Assert.Equal("gemini-audio", result.ModelCode);
        Assert.True(result.SupportsTextGeneration);
        Assert.True(result.SupportsAudioInput);
        Assert.True(result.SupportsPronunciationAssessment);
        Assert.True(result.SupportsLiveAudio);
        Assert.True(result.IsAvailable);
        Assert.True(result.IsEnabled);
        Assert.Equal("gemini-audio", result.EffectiveModelCode);
    }

    [Fact]
    public async Task FindByModelCodeAsync_UnknownModel_ReturnsNull()
    {
        await using var db = CreateContext();

        var result = await new AiModelCatalogReader(db).FindByModelCodeAsync("missing-model");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByModelCodeAsync_DeprecatedModelWithRoute_UsesEffectiveModelCode()
    {
        await using var db = CreateContext();
        var entity = new AiModelConfigEntity(
            "Deprecated Model",
            "old-model",
            routedModelCode: "replacement-model",
            isDeprecated: true);
        db.AiModelCatalog.Add(entity);
        await db.SaveChangesAsync();

        var result = await new AiModelCatalogReader(db).FindByModelCodeAsync("old-model");

        Assert.NotNull(result);
        Assert.Equal("old-model", result.ModelCode);
        Assert.Equal("replacement-model", result.EffectiveModelCode);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
