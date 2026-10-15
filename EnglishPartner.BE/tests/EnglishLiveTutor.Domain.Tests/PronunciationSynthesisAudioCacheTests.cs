using System.Security.Cryptography;
using System.Text;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class PronunciationSynthesisAudioCacheTests
{
    private sealed class FakeSynthesizer : ILivePronunciationTtsSynthesizer
    {
        public int CallCount = 0;
        public SynthesizePronunciationAudioRequest? LastRequest;
        public bool ReturnComplete = true;
        public byte[] GeneratedBytes = [1, 2, 3, 4, 5, 6, 7, 8];
        public TimeSpan ArtificialDelay = TimeSpan.Zero;

        public async Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
            SynthesizePronunciationAudioRequest request,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            LastRequest = request;

            if (ArtificialDelay > TimeSpan.Zero)
            {
                await Task.Delay(ArtificialDelay, ct);
            }

            return new SynthesizePronunciationAudioResponseDto(
                Text: request.Text ?? string.Empty,
                TargetType: request.TargetType,
                Accent: request.Accent,
                VoiceName: request.VoiceName,
                SpeechSpeed: request.SpeechSpeed,
                ModelCodeUsed: request.ModelCode,
                SystemPromptUsed: "Fake System Prompt",
                MimeType: "audio/wav",
                SampleRate: 24000,
                DurationMs: 500,
                AudioBase64: Convert.ToBase64String(GeneratedBytes),
                IsComplete: ReturnComplete
            );
        }
    }

    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(VocabularyEntity Vocab, Guid ExampleId, Guid SenseId)> SeedVocabularyAsync(AppDbContext db, string word = "scalable")
    {
        var vocab = VocabularyEntity.Create(word);
        var sense = vocab.AddSense("adjective", "Able to scale", "Dễ mở rộng", "B2");
        var example = sense.AddExample("The system is scalable.", "Hệ thống dễ mở rộng.");
        db.Vocabularies.Add(vocab);
        await db.SaveChangesAsync();
        return (vocab, example.Id, sense.Id);
    }

    [Fact]
    public async Task CacheMiss_ShouldInvokeProvider_SaveWavAndMetadata_AndSecondCallShouldHitCache()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db);

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();

        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        var command = new SynthesizePronunciationAudioCommand(new SynthesizePronunciationAudioRequest(
            ApiKey: "valid-key",
            TargetId: vocab.Id,
            TargetType: "word",
            VoiceName: "Aoede",
            SpeechSpeed: 1.0,
            Accent: "US",
            ModelCode: "gemini-3.8-live"
        ));

        // First call: cache miss
        var result1 = await handler.HandleAsync(command);
        Assert.NotNull(result1);
        Assert.Equal(1, synthesizer.CallCount);
        Assert.Equal("scalable", result1.Text);
        Assert.Equal("word", result1.TargetType);
        Assert.Equal("US", result1.Accent);
        Assert.Equal(1.0, result1.SpeechSpeed);

        // Verify persisted in DB
        var savedAudio = await db.GeneratedPronunciationAudios.FirstOrDefaultAsync(a => a.TargetId == vocab.Id);
        Assert.NotNull(savedAudio);
        Assert.Equal(vocab.Id, savedAudio!.TargetId);
        Assert.Equal("scalable", savedAudio.TextSnapshot);
        Assert.Equal(1.00m, savedAudio.SpeechSpeed);
        Assert.Equal(synthesizer.GeneratedBytes, savedAudio.AudioData);

        // Second call: cache hit
        var result2 = await handler.HandleAsync(command);
        Assert.NotNull(result2);
        Assert.Equal(1, synthesizer.CallCount); // Still 1! Provider was NOT called second time
        Assert.Equal(result1.AudioBase64, result2.AudioBase64);
    }

    [Fact]
    public async Task NewContextInstance_ShouldHitPersistentCache()
    {
        string dbName = Guid.NewGuid().ToString("N");
        Guid vocabId;

        var synthesizer = new FakeSynthesizer();
        var synthesisLock = new PronunciationSynthesisLock();

        // Instance 1: seeds and synthesizes
        await using (var db1 = CreateContext(dbName))
        {
            var (vocab, _, _) = await SeedVocabularyAsync(db1);
            vocabId = vocab.Id;
            var targetResolver1 = new PronunciationTargetResolver(db1);
            var audioRepo1 = new GeneratedPronunciationAudioRepository(db1);
            var handler1 = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver1, audioRepo1, synthesisLock);

            await handler1.HandleAsync(new(new SynthesizePronunciationAudioRequest(
                ApiKey: "key",
                TargetId: vocabId,
                TargetType: "word"
            )));
            Assert.Equal(1, synthesizer.CallCount);
        }

        // Instance 2: completely new DbContext and Handler instances
        await using (var db2 = CreateContext(dbName))
        {
            var targetResolver2 = new PronunciationTargetResolver(db2);
            var audioRepo2 = new GeneratedPronunciationAudioRepository(db2);
            var handler2 = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver2, audioRepo2, synthesisLock);

            var res = await handler2.HandleAsync(new(new SynthesizePronunciationAudioRequest(
                ApiKey: "", // No API key required for cache hit
                TargetId: vocabId,
                TargetType: "word"
            )));

            Assert.NotNull(res);
            Assert.Equal(1, synthesizer.CallCount); // Provider was NOT invoked by new instance
        }
    }

    [Fact]
    public async Task VaryingAnyKeyComponent_ShouldResultInCacheMiss_AndNotUseWrongAudio()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, exampleId, _) = await SeedVocabularyAsync(db);

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        // Initial call
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word", VoiceName: "Aoede", SpeechSpeed: 1.0, Accent: "US", ModelCode: "gemini-3.8-live"
        )));
        Assert.Equal(1, synthesizer.CallCount);

        // Different Voice
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word", VoiceName: "Puck", SpeechSpeed: 1.0, Accent: "US", ModelCode: "gemini-3.8-live"
        )));
        Assert.Equal(2, synthesizer.CallCount);

        // Different Accent
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word", VoiceName: "Aoede", SpeechSpeed: 1.0, Accent: "UK", ModelCode: "gemini-3.8-live"
        )));
        Assert.Equal(3, synthesizer.CallCount);

        // Different Speed
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word", VoiceName: "Aoede", SpeechSpeed: 0.8, Accent: "US", ModelCode: "gemini-3.8-live"
        )));
        Assert.Equal(4, synthesizer.CallCount);

        // Different Model
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word", VoiceName: "Aoede", SpeechSpeed: 1.0, Accent: "US", ModelCode: "gemini-3.8-live-extended-thinking"
        )));
        Assert.Equal(5, synthesizer.CallCount);

        // Different TargetType and TargetId (example sentence)
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: exampleId, TargetType: "sentence", VoiceName: "Aoede", SpeechSpeed: 1.0, Accent: "US", ModelCode: "gemini-3.8-live"
        )));
        Assert.Equal(6, synthesizer.CallCount);
    }

    [Fact]
    public async Task MultiWordVocabulary_ShouldBeHandledAsWordTarget()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db, word: "point of congestion");

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        var res = await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: vocab.Id,
            TargetType: "word"
        )));

        Assert.Equal("point of congestion", res.Text);
        Assert.Equal("word", res.TargetType); // Not forced into sentence even though it contains spaces!
        Assert.Equal("point of congestion", synthesizer.LastRequest?.Text);
    }

    [Fact]
    public async Task SentenceBelongingToWrongVocabularyOrSense_ShouldBeRejected()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (_, exampleId, senseId) = await SeedVocabularyAsync(db);

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        // Wrong VocabularyId context
        var wrongVocabId = Guid.NewGuid();
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: exampleId,
            TargetType: "sentence",
            VocabularyId: wrongVocabId,
            SenseId: senseId
        ))));
        Assert.Contains("VocabularyId", ex1.Message);

        // Wrong SenseId context
        var wrongSenseId = Guid.NewGuid();
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: exampleId,
            TargetType: "sentence",
            SenseId: wrongSenseId
        ))));
        Assert.Contains("SenseId", ex2.Message);
    }

    [Fact]
    public async Task SoftDeletedTarget_ShouldBeRejected()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, exampleId, _) = await SeedVocabularyAsync(db);

        vocab.SoftDelete(DateTimeOffset.UtcNow, "tester");
        await db.SaveChangesAsync();

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        // Word on deleted vocabulary
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: vocab.Id,
            TargetType: "word"
        ))));

        // Sentence belonging to deleted vocabulary
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: exampleId,
            TargetType: "sentence"
        ))));
    }

    [Fact]
    public async Task CacheHitWithoutApiKey_ShouldSucceed_AndCacheMissWithoutApiKey_ShouldFailBeforeProvider()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db);

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        // Miss with empty key -> must throw before calling provider
        var missEx = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "",
            TargetId: vocab.Id,
            TargetType: "word"
        ))));
        Assert.Contains("Google AI Studio API Key", missEx.Message);
        Assert.Equal(0, synthesizer.CallCount);

        // Generate with key
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: vocab.Id,
            TargetType: "word"
        )));
        Assert.Equal(1, synthesizer.CallCount);

        // Hit with empty key -> succeeds without calling provider
        var hitRes = await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "",
            TargetId: vocab.Id,
            TargetType: "word"
        )));
        Assert.NotNull(hitRes);
        Assert.Equal(1, synthesizer.CallCount);
    }

    [Fact]
    public async Task GetPronunciationAudioQuery_ShouldReturnAudioOnHit_AndNullOnMiss()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db);

        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var queryHandler = new GetPronunciationAudioQueryHandler(targetResolver, audioRepo);

        // Cache miss -> returns null
        var missResult = await queryHandler.HandleAsync(new GetPronunciationAudioQuery(
            TargetType: "word",
            TargetId: vocab.Id
        ));
        Assert.Null(missResult);

        // Seed cache
        var audioEntity = new GeneratedPronunciationAudioEntity(
            targetType: "word",
            targetId: vocab.Id,
            voiceName: "Aoede",
            speechSpeed: 1.0m,
            accent: "US",
            modelCode: "gemini-3.8-live",
            textSnapshot: "scalable",
            textHash: GeneratedPronunciationAudioEntity.ComputeTextHash("scalable"),
            audioData: [10, 20, 30],
            mimeType: "audio/wav",
            sampleRate: 24000,
            durationMs: 400
        );
        await audioRepo.AddAsync(audioEntity);
        await audioRepo.SaveChangesAsync();

        // Cache hit -> returns DTO
        var hitResult = await queryHandler.HandleAsync(new GetPronunciationAudioQuery(
            TargetType: "word",
            TargetId: vocab.Id
        ));
        Assert.NotNull(hitResult);
        Assert.Equal("scalable", hitResult!.Text);
        Assert.Equal(Convert.ToBase64String([10, 20, 30]), hitResult.AudioBase64);
    }

    [Fact]
    public async Task PartialOrTimeoutAudio_ShouldNotBeSavedToCache()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db);

        var synthesizer = new FakeSynthesizer { ReturnComplete = false }; // partial audio returned!
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        var res = await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: vocab.Id,
            TargetType: "word"
        )));

        Assert.NotNull(res);
        Assert.False(res.IsComplete);
        Assert.Equal(1, synthesizer.CallCount);

        // Verify NOT stored in cache
        var cached = await db.GeneratedPronunciationAudios.FirstOrDefaultAsync(a => a.TargetId == vocab.Id);
        Assert.Null(cached);

        // Second call should still miss and call provider
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key",
            TargetId: vocab.Id,
            TargetType: "word"
        )));
        Assert.Equal(2, synthesizer.CallCount);
    }

    [Fact]
    public async Task SingleFlightConcurrency_ShouldCallProviderOnceForSameKey_AndAllowDifferentKeys()
    {
        string dbName = Guid.NewGuid().ToString("N");
        Guid vocab1Id;
        Guid vocab2Id;
        await using (var db = CreateContext(dbName))
        {
            var (vocab1, _, _) = await SeedVocabularyAsync(db, "scalable");
            var (vocab2, _, _) = await SeedVocabularyAsync(db, "resilient");
            vocab1Id = vocab1.Id;
            vocab2Id = vocab2.Id;
        }

        var synthesizer = new FakeSynthesizer { ArtificialDelay = TimeSpan.FromMilliseconds(50) };
        var synthesisLock = new PronunciationSynthesisLock();

        async Task<SynthesizePronunciationAudioResponseDto> ExecuteRequestAsync(Guid targetId)
        {
            await using var db = CreateContext(dbName);
            var targetResolver = new PronunciationTargetResolver(db);
            var audioRepo = new GeneratedPronunciationAudioRepository(db);
            var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);
            return await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(ApiKey: "key", TargetId: targetId, TargetType: "word")));
        }

        // Run 4 concurrent requests for vocab1
        var task1 = ExecuteRequestAsync(vocab1Id);
        var task2 = ExecuteRequestAsync(vocab1Id);
        var task3 = ExecuteRequestAsync(vocab1Id);
        var task4 = ExecuteRequestAsync(vocab1Id);

        // Also run a request for vocab2 concurrently (different key)
        var taskVocab2 = ExecuteRequestAsync(vocab2Id);

        await Task.WhenAll(task1, task2, task3, task4, taskVocab2);

        // Vocab 1 provider should only be called ONCE despite 4 concurrent callers
        // Vocab 2 provider called once
        // Total calls = 2!
        Assert.Equal(2, synthesizer.CallCount);
    }

    [Fact]
    public void EfCoreModelMetadata_ShouldHaveValueGeneratedNeverAndRequiredIndexes()
    {
        using var db = CreateContext(Guid.NewGuid().ToString("N"));
        var entityType = db.Model.FindEntityType(typeof(GeneratedPronunciationAudioEntity));
        Assert.NotNull(entityType);

        var idProp = entityType!.FindProperty(nameof(GeneratedPronunciationAudioEntity.Id));
        Assert.NotNull(idProp);
        Assert.Equal(ValueGenerated.Never, idProp!.ValueGenerated);

        // Check indexes
        var indexes = entityType.GetIndexes().ToList();

        // 4 root columns composite index
        var fourColIndex = indexes.FirstOrDefault(i =>
            i.Properties.Count == 4 &&
            i.Properties[0].Name == nameof(GeneratedPronunciationAudioEntity.TargetType) &&
            i.Properties[1].Name == nameof(GeneratedPronunciationAudioEntity.VoiceName) &&
            i.Properties[2].Name == nameof(GeneratedPronunciationAudioEntity.SpeechSpeed) &&
            i.Properties[3].Name == nameof(GeneratedPronunciationAudioEntity.TargetId));
        Assert.NotNull(fourColIndex);

        // Full lookup index
        var lookupIndex = indexes.FirstOrDefault(i => i.GetDatabaseName() == "IX_GeneratedPronunciationAudios_Lookup");
        Assert.NotNull(lookupIndex);
        Assert.Equal(7, lookupIndex!.Properties.Count);
    }

    [Fact]
    public async Task TextChange_ShouldInvalidateCache_EvenIfRootKeyMatches()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db, "resilient");

        var synthesizer = new FakeSynthesizer();
        var targetResolver = new PronunciationTargetResolver(db);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesisLock = new PronunciationSynthesisLock();
        var handler = new SynthesizePronunciationAudioCommandHandler(synthesizer, targetResolver, audioRepo, synthesisLock);

        // Generate audio for word "resilient"
        await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word"
        )));
        Assert.Equal(1, synthesizer.CallCount);

        // Update word in DB from "resilient" to "resilience"
        vocab.UpdateDetails("resilience", null, null);
        await db.SaveChangesAsync();

        // Next request with same TargetId should NOT hit the old cached audio because text hash changed!
        var res = await handler.HandleAsync(new(new SynthesizePronunciationAudioRequest(
            ApiKey: "key", TargetId: vocab.Id, TargetType: "word"
        )));

        Assert.Equal(2, synthesizer.CallCount);
        Assert.Equal("resilience", res.Text);
    }

    [Fact]
    public async Task LatestValidAudio_ShouldBeSelected_AndCorruptOrInvalidAudioSkipped()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var (vocab, _, _) = await SeedVocabularyAsync(db);

        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        string textHash = GeneratedPronunciationAudioEntity.ComputeTextHash("scalable");

        var t0 = new DateTimeOffset(2026, 10, 15, 1, 0, 0, TimeSpan.Zero);
        var olderValidAudio = new GeneratedPronunciationAudioEntity(
            targetType: "word",
            targetId: vocab.Id,
            voiceName: "Aoede",
            speechSpeed: 1.0m,
            accent: "US",
            modelCode: "gemini-3.8-live",
            textSnapshot: "scalable",
            textHash: textHash,
            audioData: [1, 2, 3],
            isValid: true,
            id: Guid.NewGuid()
        );
        olderValidAudio.SetCreationAudit(t0);

        var t1 = t0.AddHours(1);
        var newerInvalidAudio = new GeneratedPronunciationAudioEntity(
            targetType: "word",
            targetId: vocab.Id,
            voiceName: "Aoede",
            speechSpeed: 1.0m,
            accent: "US",
            modelCode: "gemini-3.8-live",
            textSnapshot: "scalable",
            textHash: textHash,
            audioData: [4, 5, 6],
            isValid: false, // Invalidated
            id: Guid.NewGuid()
        );
        newerInvalidAudio.SetCreationAudit(t1);

        db.GeneratedPronunciationAudios.AddRange(olderValidAudio, newerInvalidAudio);
        await db.SaveChangesAsync();

        var selected = await audioRepo.GetValidAudioAsync("word", vocab.Id, "Aoede", 1.0m, "US", "gemini-3.8-live", textHash);

        Assert.NotNull(selected);
        Assert.Equal(olderValidAudio.Id, selected!.Id);
        Assert.Equal([1, 2, 3], selected.AudioData);
    }

    [Fact]
    public async Task SynthesizePronunciationAudioRequest_JsonDeserialization_ShouldSafelyHandleNonGuidAndEmptyStrings()
    {
        string jsonWithNonGuid = """
        {
            "apiKey": "test-key",
            "text": "Stateless backend architecture",
            "targetType": "sentence",
            "accent": "US",
            "voiceName": "Aoede",
            "speechSpeed": 1.0,
            "modelCode": "gemini-3.8-live",
            "targetId": "ex1",
            "vocabularyId": "",
            "senseId": "invalid-guid"
        }
        """;

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var req = System.Text.Json.JsonSerializer.Deserialize<SynthesizePronunciationAudioRequest>(jsonWithNonGuid, options);

        Assert.NotNull(req);
        Assert.Null(req!.TargetId);
        Assert.Null(req.VocabularyId);
        Assert.Null(req.SenseId);
        Assert.Equal("Stateless backend architecture", req.Text);

        // Verify it can execute cleanly through handler as ad-hoc text synthesis
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateContext(dbName);
        var audioRepo = new GeneratedPronunciationAudioRepository(db);
        var synthesizer = new FakeSynthesizer();
        var lockService = new PronunciationSynthesisLock();
        var targetResolver = new PronunciationTargetResolver(db);

        var handler = new SynthesizePronunciationAudioCommandHandler(
            synthesizer,
            targetResolver,
            audioRepo,
            lockService
        );

        var res = await handler.HandleAsync(new(req));
        Assert.NotNull(res);
        Assert.Equal("Stateless backend architecture", res.Text);
        Assert.Equal(1, synthesizer.CallCount);
    }
}

