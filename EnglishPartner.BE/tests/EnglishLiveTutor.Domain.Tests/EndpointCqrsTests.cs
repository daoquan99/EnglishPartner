using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Domain.Tests;

public class EndpointCqrsTests
{
    private sealed class TestDbFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AuditingSaveChangesInterceptor())
            .Options;

        public AppDbContext Create() => new(_options);

        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult<IAppDbContext>(Create());
    }

    [Fact]
    public async Task VocabularyUpdate_ShouldPersistInPlace_AndKeepOtherSenses()
    {
        var factory = new TestDbFactory();
        await using var db = factory.Create();
        var repository = new VocabularyRepository(db);
        var create = new CreateVocabularyCommandHandler(repository);
        var created = await create.HandleAsync(new(new UpsertVocabularyAdminRequest("resilient",
            DefinitionEn: "Able to recover", MeaningVi: "Kiên cường",
            Synonyms: ["strong"], Examples: [new("She is resilient.", "Cô ấy kiên cường.")])));
        var vocabulary = (await repository.GetByIdWithDetailsAsync(created.VocabularyId))!;
        var secondSense = vocabulary.AddSense("noun", "Another meaning", "Nghĩa khác", isPrimary: false);
        await db.SaveChangesAsync();
        var creationTime = vocabulary.CreatedAtUtc;
        var oldExampleId = vocabulary.Senses.First(s => s.Id == created.SenseId).Examples.Single().Id;

        var update = new UpdateVocabularyCommandHandler(repository);
        var updated = await update.HandleAsync(new(created.VocabularyId,
            new UpsertVocabularyAdminRequest("resilience", PartOfSpeech: "noun", CefrLevel: "C1",
                DefinitionEn: "Ability to recover", MeaningVi: "Sự kiên cường", TopicCategory: "Work",
                Synonyms: ["toughness"], WordFamilies: [new("resilient", "adjective")],
                Examples: [new("Resilience matters.", "Sự kiên cường quan trọng.")])));

        Assert.NotNull(updated);
        Assert.Equal(created.VocabularyId, updated.VocabularyId);
        Assert.Equal(created.SenseId, updated.SenseId);
        db.ChangeTracker.Clear();
        var stored = (await repository.GetByIdWithDetailsAsync(created.VocabularyId))!;
        Assert.False(stored.IsDeleted);
        Assert.Equal(creationTime, stored.CreatedAtUtc);
        Assert.Equal("resilience", stored.Word);
        Assert.Equal(2, stored.Senses.Count);
        Assert.Contains(stored.Senses, s => s.Id == secondSense.Id);
        var primary = stored.Senses.Single(s => s.Id == created.SenseId);
        Assert.Equal("C1", primary.CefrLevel);
        Assert.Equal("toughness", primary.Relations.Single().RelatedWord);
        Assert.Single(primary.Examples);
        Assert.False(await db.VocabularyExamples.AnyAsync(e => e.Id == oldExampleId));
        Assert.Equal("resilient", stored.WordFamilies.Single().RelatedWord);
    }

    [Fact]
    public async Task VocabularyQuery_ShouldPageSensesAfterFiltering_AndExcludeDeletedRoots()
    {
        var factory = new TestDbFactory();
        await using (var db = factory.Create())
        {
            var vocabulary = VocabularyEntity.Create("scale", topicCategory: "Engineering");
            vocabulary.AddSense("noun", "Size", "Quy mô", "B2");
            vocabulary.AddSense("verb", "Grow", "Mở rộng", "B2");
            vocabulary.AddSense("noun", "Musical scale", "Âm giai", "C1");
            var deleted = VocabularyEntity.Create("deleted", topicCategory: "Engineering");
            deleted.AddSense("noun", "Hidden", "Ẩn", "B2");
            deleted.SoftDelete(DateTimeOffset.UtcNow);
            db.AddRange(vocabulary, deleted);
            await db.SaveChangesAsync();
        }

        var handler = new ListVocabularyFlashcardsQueryHandler(factory);
        var first = await handler.HandleAsync(new(CefrLevel: "b2", TopicCategory: "Engineering", Page: 1, PageSize: 1));
        var second = await handler.HandleAsync(new(CefrLevel: "b2", TopicCategory: "Engineering", Page: 2, PageSize: 1));
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.NotEqual(first.Items[0].SenseId, second.Items[0].SenseId);
        Assert.All(first.Items.Concat(second.Items), item => Assert.Equal("B2", item.CefrLevel));
        var pastEnd = await handler.HandleAsync(new(CefrLevel: "B2", Page: 3, PageSize: 1));
        Assert.Empty(pastEnd.Items);
        Assert.Equal(2, pastEnd.TotalCount);
        var searched = await handler.HandleAsync(new(Search: "mở rộng"));
        Assert.Equal("verb", Assert.Single(searched.Items).PartOfSpeech);
    }

    [Fact]
    public async Task AiModelCommands_ShouldUpdateExistingRow_AndPersistAuditedSoftDelete()
    {
        var factory = new TestDbFactory();
        await using var db = factory.Create();
        var entity = new AiModelConfigEntity("Before", "test-model", supportsTts: true);
        db.AiModelCatalog.Add(entity);
        await db.SaveChangesAsync();
        var createdAt = entity.CreatedAtUtc;
        var repo = new AiModelCatalogRepository(db);
        var update = new UpdateAiModelConfigCommandHandler(repo);
        var updated = await update.HandleAsync(new(entity.Id,
            new UpsertAiModelConfigRequest("After", "test-model-v2", SupportsTranslation: true)));
        Assert.Same(entity, updated);
        db.ChangeTracker.Clear();
        var stored = (await repo.GetByIdAsync(entity.Id))!;
        Assert.Equal("After", stored.DisplayName);
        Assert.True(stored.SupportsTts);
        Assert.True(stored.SupportsTranslation);
        Assert.Equal(createdAt, stored.CreatedAtUtc);

        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));
        var delete = new DeleteAiModelConfigCommandHandler(repo, new FakeCurrentUser { UserId = "admin-test" }, clock);
        Assert.True(await delete.HandleAsync(new(entity.Id)));
        db.ChangeTracker.Clear();
        Assert.Null(await repo.GetByIdAsync(entity.Id));
        var deleted = await db.AiModelCatalog.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("admin-test", deleted.DeletedBy);
        Assert.Equal(clock.GetUtcNow(), deleted.DeletedAtUtc);
        Assert.False(await delete.HandleAsync(new(entity.Id)));
    }

    [Fact]
    public async Task SpeechSpeedCommands_ShouldRestoreExistingCode_AndKeepOneDefault()
    {
        var factory = new TestDbFactory();
        await using var db = factory.Create();
        var repo = new SpeechSpeedRepository(db);
        var handler = new UpsertSpeechSpeedCommandHandler(repo);
        var original = await handler.HandleAsync(new(new("normal", 1, "Normal", "", IsDefault: true)));
        var slow = await handler.HandleAsync(new(new("slow", 0.8, "Slow", "", IsDefault: true)));
        Assert.Equal(slow.Id, (await db.SpeechSpeeds.SingleAsync(s => s.IsDefault)).Id);
        var delete = new DeleteSpeechSpeedCommandHandler(repo, new FakeCurrentUser(), TimeProvider.System);
        Assert.True(await delete.HandleAsync(new(original.Id)));
        var restored = await handler.HandleAsync(new(new("normal", 1.1, "Restored", "", IsDefault: true)));
        Assert.Equal(original.Id, restored.Id);
        db.ChangeTracker.Clear();
        var stored = (await repo.GetByIdAsync(original.Id))!;
        Assert.Equal(1.1, stored.SpeedValue);
        Assert.True(stored.IsActive);
        Assert.Equal(original.Id, (await db.SpeechSpeeds.SingleAsync(s => s.IsDefault)).Id);
    }

    [Fact]
    public async Task SpeechSpeedQuery_ShouldNotReturnDefaultsForEmptyOrPastEndPages()
    {
        var factory = new TestDbFactory();
        var handler = new ListSpeechSpeedsQueryHandler(factory);
        Assert.Empty((await handler.HandleAsync(new())).Items);
        await using (var db = factory.Create())
        {
            db.SpeechSpeeds.AddRange(new SpeechSpeedEntity("slow", 0.8, "Slow", ""),
                new SpeechSpeedEntity("normal", 1, "Normal", ""),
                new SpeechSpeedEntity("off", 1.2, "Inactive", "", isActive: false));
            await db.SaveChangesAsync();
        }
        var page = await handler.HandleAsync(new(Page: 2, PageSize: 1));
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
        var missing = await handler.HandleAsync(new(Search: "absent"));
        Assert.Empty(missing.Items);
        var pastEnd = await handler.HandleAsync(new(Page: 3, PageSize: 1));
        Assert.Empty(pastEnd.Items);
        Assert.Equal(2, pastEnd.TotalCount);
    }

    [Fact]
    public async Task PromptAndRubricDeleteCommands_ShouldPersistAndHideDeletedEntities()
    {
        var factory = new TestDbFactory();
        await using var db = factory.Create();
        db.AddRange(new CefrLevelPromptEntity("B1", "B1", "Guidance"),
            new ConversationModePromptEntity("practice", "General", "Practice", "Directive"),
            new ScoringRubricEntity("RUBRIC_TEST", "Test"));
        await db.SaveChangesAsync();
        var promptRepo = new PromptConfigurationRepository(db);
        var actor = new FakeCurrentUser();
        Assert.True(await new DeleteCefrLevelPromptCommandHandler(promptRepo, actor, TimeProvider.System).HandleAsync(new("b1")));
        Assert.True(await new DeleteConversationModePromptCommandHandler(promptRepo, actor, TimeProvider.System).HandleAsync(new("PRACTICE")));
        Assert.True(await new DeleteScoringRubricCommandHandler(new ScoringRubricRepository(db), actor, TimeProvider.System).HandleAsync(new("RUBRIC_TEST")));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.CefrLevelPrompts.ToListAsync());
        Assert.Empty(await db.ConversationModePrompts.ToListAsync());
        Assert.Empty(await db.ScoringRubrics.ToListAsync());
        Assert.True((await db.CefrLevelPrompts.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }
}
