using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;
using EnglishLiveTutor.Application.Features.Lessons.Queries.MediaQueryOperations;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Application.Features.Prompts.Services;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Tests.TestDoubles;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AudioAndPromptContractTests
{
    [Fact]
    public async Task AudioRecordingStore_PreservesOwnerAndDefersCommit()
    {
        await using var db = CreateDb();
        var store = new AudioRecordingStore(new AudioRecordingRepository(db));

        var recording = await store.AddAsync(
            userId: "owner-user",
            storageKey: "media/owner.mp3",
            contentType: "audio/mpeg",
            fileSizeBytes: 123,
            durationMilliseconds: 456);

        Assert.Equal("owner-user", recording.UserId);
        Assert.Empty(db.AudioRecordings);

        await db.SaveChangesAsync();
        var persisted = await store.GetByIdAsync(recording.Id);
        Assert.NotNull(persisted);
        Assert.Equal("owner-user", persisted!.UserId);
        Assert.Equal("media/owner.mp3", persisted.StorageKey);
    }

    [Fact]
    public async Task AudioRecordingStore_CallerStillRejectsAnotherUser()
    {
        await using var db = CreateDb();
        var store = new AudioRecordingStore(new AudioRecordingRepository(db));
        var recording = await store.AddAsync(
            "owner-user",
            "media/owner.mp3",
            "audio/mpeg",
            123,
            456);
        await db.SaveChangesAsync();

        var handler = new GetLessonMediaQueryHandler(store, new User("another-user"));
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(new GetLessonMediaQuery(recording.Id)));

        Assert.Null(await store.GetOwnedAsync(recording.Id, "another-user"));
        Assert.Null(await store.GetOwnedAsync(Guid.NewGuid(), "owner-user"));
        Assert.NotNull(await store.GetOwnedAsync(recording.Id, "OWNER-USER"));
    }

    [Fact]
    public async Task PronunciationObserver_UserA_DoesNotUpdateUserBForSameSense()
    {
        await using var db = CreateDb();
        var vocabularyId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var userAProgress = new UserVocabularyProgressEntity("user-a", vocabularyId, senseId);
        var userBProgress = new UserVocabularyProgressEntity("user-b", vocabularyId, senseId);
        userAProgress.UpdateLatestPronunciationScore(50m);
        userBProgress.UpdateLatestPronunciationScore(70m);
        db.UserVocabularyProgresses.AddRange(userAProgress, userBProgress);
        await db.SaveChangesAsync();

        await new VocabularyPronunciationProgressObserver(db)
            .OnAssessedAsync("user-a", senseId, 90m);
        await db.SaveChangesAsync();

        Assert.Equal(90m, userAProgress.LatestPronunciationScore);
        Assert.Equal(70m, userBProgress.LatestPronunciationScore);
    }

    [Fact]
    public async Task PronunciationAudioSynthesizer_CacheHitDoesNotCallProvider()
    {
        await using var db = CreateDb();
        var repository = new GeneratedPronunciationAudioRepository(db);
        var text = "cached word";
        var entity = new GeneratedPronunciationAudioEntity(
            targetType: "word",
            targetId: Guid.NewGuid(),
            voiceName: "Aoede",
            speechSpeed: 1.00m,
            accent: "US",
            modelCode: "gemini-3.8-live",
            textSnapshot: text,
            textHash: GeneratedPronunciationAudioEntity.ComputeTextHash(text),
            audioData: [1, 2, 3],
            mimeType: "audio/wav",
            sampleRate: 24000,
            durationMs: 100);
        db.GeneratedPronunciationAudios.Add(entity);
        await db.SaveChangesAsync();

        var provider = new CountingTtsSynthesizer();
        var synthesizer = new PronunciationAudioSynthesizer(
            provider,
            repository,
            new PronunciationSynthesisLock());

        var result = await synthesizer.SynthesizeAsync(new PronunciationAudioSynthesisRequest(
            ApiKey: null,
            Text: text,
            TargetType: "word",
            TargetId: entity.TargetId,
            VoiceName: "Aoede",
            SpeechSpeed: 1.0,
            Accent: "US",
            ModelCode: "gemini-3.8-live",
            MissingApiKeyMessage: "missing",
            IncompleteAudioMessage: "incomplete"));

        Assert.Equal(Convert.ToBase64String([1, 2, 3]), result.AudioBase64);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task PronunciationAudioSynthesizer_DefaultMode_PreservesProviderBase64()
    {
        await using var db = CreateDb();
        var provider = new RecordingTtsSynthesizer();
        var synthesizer = new PronunciationAudioSynthesizer(
            provider,
            new GeneratedPronunciationAudioRepository(db),
            new PronunciationSynthesisLock());

        var result = await synthesizer.SynthesizeAsync(
            CreateAudioSynthesisRequest() with { ApiKey = "test-only" });

        Assert.Equal(Convert.ToBase64String([1, 2, 3]) + "\n", result.AudioBase64);
    }

    [Fact]
    public void PronunciationAudioSynthesisRequest_ToString_MasksApiKey()
    {
        var request = CreateAudioSynthesisRequest() with
        {
            ApiKey = "super-secret-key"
        };

        string text = request.ToString();

        Assert.Contains("ApiKey = ***", text);
        Assert.DoesNotContain("super-secret-key", text);
    }

    [Fact]
    public async Task LessonSynthesis_PreservesCacheTargetTypeAndUsesProviderTargetType()
    {
        await using var db = CreateDb();
        var lesson = LessonEntity.Create("Daily routine", "A2", "Listening");
        var exercise = lesson.AddExercise(
            "one",
            ExerciseType.Dictation,
            "Practice",
            "Listen",
            5,
            GradingMode.Deterministic,
            "{}",
            """{"transcript":"I work from home."}""");
        var attempt = LessonAttemptEntity.Start(lesson.Id, "learner", 1, 5);
        db.Lessons.Add(lesson);
        db.LessonAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var provider = new RecordingTtsSynthesizer();
        var synthesizer = new PronunciationAudioSynthesizer(
            provider,
            new GeneratedPronunciationAudioRepository(db),
            new PronunciationSynthesisLock());
        var handler = new SynthesizeExerciseAudioCommandHandler(
            db,
            new User("learner"),
            synthesizer);

        var result = await handler.HandleAsync(new(
            attempt.Id,
            exercise.Id,
            new SynthesizeExerciseAudioRequest(ApiKey: "test-only")));

        Assert.Equal("lesson-exercise", db.GeneratedPronunciationAudios.Single().TargetType);
        Assert.Equal("sentence", provider.LastRequest?.TargetType);
        Assert.Equal(Convert.ToBase64String([1, 2, 3]), result.AudioBase64);

        await handler.HandleAsync(new(
            attempt.Id,
            exercise.Id,
            new SynthesizeExerciseAudioRequest()));

        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task PromptCatalogReader_UsesActiveCatalogRowsAndRendersModeDirective()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.CefrLevelPrompts.Add(new CefrLevelPromptEntity(
                "B2",
                "B2",
                "ACTIVE_B2_GUIDANCE"));
            seed.ConversationModePrompts.Add(new ConversationModePromptEntity(
                "english_coach",
                "Coaching",
                "Coach",
                "Coach at {cefrLevel}"));
            await seed.SaveChangesAsync();
        }

        var reader = new PromptCatalogReader(
            new ModuleDbContextFactoryStub<ICatalogDbContext>(
                () => new AppDbContext(options)));
        var snapshot = await reader.GetActiveAsync("B2", "english_coach");

        Assert.Equal("ACTIVE_B2_GUIDANCE", snapshot.LevelGuidance);
        Assert.Equal("Coach at B2", snapshot.ModeDirective);
    }

    [Fact]
    public void PromptCatalogFallbacks_MatchSeederEntities()
    {
        var levels = PromptCatalogEntityDefaults.GetDefaultCefrLevelPrompts();
        foreach (var level in levels)
        {
            Assert.Equal(
                level.LevelGuidance,
                PromptCatalogDefaults.GetLevelGuidance(level.CefrLevel));
        }

        var modes = PromptCatalogEntityDefaults.GetDefaultConversationModePrompts();
        foreach (var mode in modes)
        {
            Assert.Equal(
                mode.RenderDirective("B2"),
                PromptCatalogDefaults
                    .GetFallback("B2", mode.ModeCode)
                    .ModeDirective);
        }
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static PronunciationAudioSynthesisRequest CreateAudioSynthesisRequest() =>
        new(
            ApiKey: null,
            Text: "hello",
            TargetType: "word",
            TargetId: Guid.NewGuid(),
            VoiceName: "Aoede",
            SpeechSpeed: 1.0,
            Accent: "US",
            ModelCode: "gemini-3.8-live",
            MissingApiKeyMessage: "missing",
            IncompleteAudioMessage: "incomplete");

    private sealed record User(string? UserId) : ICurrentUser;

    private sealed class CountingTtsSynthesizer : ILivePronunciationTtsSynthesizer
    {
        public int CallCount { get; private set; }

        public Task<EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
            EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioRequest request,
            CancellationToken ct = default)
        {
            CallCount++;
            throw new InvalidOperationException("Provider must not be called on a cache hit.");
        }
    }

    private sealed class RecordingTtsSynthesizer : ILivePronunciationTtsSynthesizer
    {
        public int CallCount { get; private set; }

        public EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
            EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioRequest request,
            CancellationToken ct = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new EnglishLiveTutor.Application.Common.Models.SynthesizePronunciationAudioResponseDto(
                Text: request.Text ?? string.Empty,
                TargetType: request.TargetType,
                Accent: request.Accent,
                VoiceName: request.VoiceName,
                SpeechSpeed: request.SpeechSpeed,
                ModelCodeUsed: request.ModelCode,
                SystemPromptUsed: string.Empty,
                MimeType: "audio/wav",
                SampleRate: 24000,
                DurationMs: 100,
                AudioBase64: Convert.ToBase64String([1, 2, 3]) + "\n",
                IsComplete: true));
        }
    }
}
