using System.Text.Json;
using EnglishLiveTutor.Application;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.AiModels.Services;
using EnglishLiveTutor.Application.Features.Identity.Services;
using EnglishLiveTutor.Application.Features.Learning.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Domain.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class AiCommandsUnitOfWorkTests
{
    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        public int CommitCount { get; private set; }

        public CountingUnitOfWork(IUnitOfWork inner)
        {
            _inner = inner;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            CommitCount++;
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken ct = default)
        {
            return _inner.ExecuteInTransactionAsync(operation, ct);
        }
    }

    private sealed class InMemoryDbFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public InMemoryDbFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IAppDbContext>(new AppDbContext(_options));
        }
    }

    private sealed class FakePronunciationAiAssessor : IPronunciationAiAssessor
    {
        private readonly Func<Task<PronunciationAiRawResult>>? _handler;
        public Action? OnAssessCalled { get; set; }

        public FakePronunciationAiAssessor(Func<Task<PronunciationAiRawResult>>? handler = null)
        {
            _handler = handler;
        }

        public async Task<PronunciationAiRawResult> AssessAudioAsync(
            string apiKey,
            string modelCode,
            string base64Audio,
            string contentType,
            string expectedText,
            string accent,
            ScoringRubricEntity rubric,
            CancellationToken ct = default)
        {
            OnAssessCalled?.Invoke();

            if (_handler != null)
                return await _handler();

            var criterionScores = new Dictionary<string, (decimal RawScore, string Feedback)>();
            foreach (var c in rubric.Criteria)
            {
                criterionScores[c.Code] = (90m, "Good pronunciation");
            }

            return new PronunciationAiRawResult(
                RecognizedText: expectedText,
                Confidence: 0.95m,
                IsAssessable: true,
                CriterionScores: criterionScores,
                Strengths: new List<string> { "Clear stress" },
                Improvements: new List<string>(),
                SuggestedPractice: new List<string>());
        }
    }

    private sealed class FakeVocabularyAiGenerator : IVocabularyAiGenerator
    {
        private readonly Func<Task<List<GeneratedVocabularyRawSense>>>? _handler;

        public FakeVocabularyAiGenerator(Func<Task<List<GeneratedVocabularyRawSense>>>? handler = null)
        {
            _handler = handler;
        }

        public async Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
            VocabularyGenerationRequestOptions options,
            CancellationToken ct = default)
        {
            if (_handler != null)
                return await _handler();

            return new List<GeneratedVocabularyRawSense>
            {
                new(
                    Word: "innovative",
                    PartOfSpeech: "adjective",
                    CefrLevel: options.CefrLevel,
                    DefinitionEn: "Featuring new methods or advanced ideas.",
                    MeaningVi: "Đổi mới, sáng tạo",
                    DescriptionEn: "Common in business and tech contexts",
                    IpaUs: "/ˈɪn.ə.veɪ.tɪv/",
                    IpaUk: "/ˈɪn.ə.və.tɪv/",
                    Synonyms: new List<string> { "inventive", "creative" },
                    Antonyms: new List<string> { "traditional" },
                    WordFamilies: new List<GeneratedWordFamilyRaw> { new("innovate", "verb") },
                    Collocations: new List<GeneratedCollocationRaw> { new("innovative approach", "cách tiếp cận đổi mới") },
                    Examples: new List<GeneratedExampleRaw> { new("They proposed an innovative solution.", "Họ đã đề xuất một giải pháp đổi mới.") },
                    GrammarPatterns: new List<GeneratedGrammarPatternRaw> { new("be innovative in", "sáng tạo trong việc gì", "She is innovative in her teaching.") }
                )
            };
        }
    }

    #region 1. Reflection Check: Không handler nào thuộc Pronunciation/Vocabulary Commands còn phụ thuộc IAppDbContextFactory

    [Fact]
    public void NoCommandHandlersInPronunciationAndVocabularyCommands_ShouldDependOn_IAppDbContextFactory()
    {
        const int preP3CommandHandlerCount = 9;
        var commandHandlerTypes = ApplicationAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Distinct()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.Namespace != null &&
                        (t.Namespace.StartsWith("EnglishLiveTutor.Application.Features.Pronunciation.Commands") ||
                         t.Namespace.StartsWith("EnglishLiveTutor.Application.Features.Vocabulary.Commands")) &&
                        t.Name.EndsWith("CommandHandler"))
            .ToList();

        Assert.True(
            commandHandlerTypes.Count >= preP3CommandHandlerCount,
            $"Expected at least {preP3CommandHandlerCount} command handlers from the pre-P3 scope, " +
            $"but found {commandHandlerTypes.Count}.");

        foreach (var handlerType in commandHandlerTypes)
        {
            var ctors = handlerType.GetConstructors();
            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                var hasDbFactory = parameters.Any(p => p.ParameterType == typeof(IAppDbContextFactory));
                Assert.False(hasDbFactory, $"{handlerType.Name} constructor still depends on IAppDbContextFactory!");
            }
        }

        // VocabularyGenerationWriter uses its module context and factory for isolated failure recording.
        var writerCtors = typeof(VocabularyGenerationWriter).GetConstructors();
        Assert.Single(writerCtors);
        var writerParams = writerCtors[0].GetParameters();
        Assert.Contains(writerParams, p => p.ParameterType == typeof(IVocabularyDbContext));
        Assert.Contains(writerParams, p => p.ParameterType == typeof(IUnitOfWork));
        Assert.Contains(
            writerParams,
            p => p.ParameterType == typeof(IModuleDbContextFactory<IVocabularyDbContext>));
    }

    #endregion

    #region 2. AssessPronunciation Tests

    [Fact]
    public async Task AssessPronunciation_CallsProviderBeforeWriting_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);

        // Seed AI model
        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true);
        db.AiModelCatalog.Add(model);

        // Seed Vocabulary
        var vocab = VocabularyEntity.Create("resilient");
        var sense = vocab.AddSense("adjective", "Able to recover", "Kiên cường", "B2");
        db.Vocabularies.Add(vocab);

        // Seed Rubric
        var rubric = new ScoringRubricEntity("ENG_PRON_V1", "English Pronunciation Rubric", assessmentType: "Pronunciation");
        rubric.AddCriterion("ACCURACY", "Accuracy", "Pronunciation accuracy", weight: 100m);
        db.ScoringRubrics.Add(rubric);

        await db.SaveChangesAsync();

        bool wasCalledBeforeWrites = false;
        var fakeAssessor = new FakePronunciationAiAssessor();
        fakeAssessor.OnAssessCalled = () =>
        {
            // Check the change tracker, not the database: nothing may even be staged before the provider runs.
            wasCalledBeforeWrites = !db.ChangeTracker.Entries<AudioRecordingEntity>().Any()
                && !db.ChangeTracker.Entries<PronunciationAssessmentEntity>().Any();
        };

        var handler = new AssessPronunciationCommandHandler(
            db,
            countingUow,
            fakeAssessor,
            new AiModelCatalogReader(db),
            new PronunciationTargetTextSource(db),
            new VocabularyPronunciationProgressObserver(db));

        string sampleAudioBase64 = Convert.ToBase64String(new byte[1024]);
        var cmd = new AssessPronunciationCommand("learner-user-id", new SubmitPronunciationAssessmentRequest(
            ApiKey: "test-gemini-key",
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            TargetType: "Word",
            AssessmentModelCode: "gemini-3.8-flash",
            Base64Audio: sampleAudioBase64,
            ContentType: "audio/webm",
            DurationMilliseconds: 1500
        ));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.True(wasCalledBeforeWrites, "Provider must be called BEFORE any audio recording or assessment is persisted.");
        Assert.Equal(1, countingUow.CommitCount);
        Assert.Equal("Completed", result.Status);
        Assert.NotNull(result.OverallScore);

        await using var verifyDb = new AppDbContext(options);
        var savedAssessment = await verifyDb.PronunciationAssessments.FirstOrDefaultAsync(a => a.VocabularyId == vocab.Id);
        Assert.NotNull(savedAssessment);
        var savedRecording = await verifyDb.AudioRecordings.FirstOrDefaultAsync(r => r.UserId == "learner-user-id");
        Assert.NotNull(savedRecording);
    }

    [Fact]
    public async Task AssessPronunciation_ProviderThrows_NoAssessmentOrRecordingSaved_ZeroCommits()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);

        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true);
        db.AiModelCatalog.Add(model);

        var vocab = VocabularyEntity.Create("tenacious");
        var sense = vocab.AddSense("adjective", "Persistent", "Kiên trì", "B2");
        db.Vocabularies.Add(vocab);

        var rubric = new ScoringRubricEntity("ENG_PRON_V1", "English Pronunciation Rubric", assessmentType: "Pronunciation");
        rubric.AddCriterion("ACCURACY", "Accuracy", "Pronunciation accuracy", weight: 100m);
        db.ScoringRubrics.Add(rubric);

        await db.SaveChangesAsync();

        var failingAssessor = new FakePronunciationAiAssessor(() => throw new HttpRequestException("Gemini service unavailable"));
        var handler = new AssessPronunciationCommandHandler(
            db,
            countingUow,
            failingAssessor,
            new AiModelCatalogReader(db),
            new PronunciationTargetTextSource(db),
            new VocabularyPronunciationProgressObserver(db));

        string sampleAudioBase64 = Convert.ToBase64String(new byte[1024]);
        var cmd = new AssessPronunciationCommand("learner-user-id", new SubmitPronunciationAssessmentRequest(
            ApiKey: "test-gemini-key",
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            TargetType: "Word",
            AssessmentModelCode: "gemini-3.8-flash",
            Base64Audio: sampleAudioBase64,
            ContentType: "audio/webm",
            DurationMilliseconds: 1500
        ));

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(cmd));

        // Commit count must be 0
        Assert.Equal(0, countingUow.CommitCount);

        // Database must NOT contain any assessment or recording
        await using var verifyDb = new AppDbContext(options);
        Assert.Empty(await verifyDb.PronunciationAssessments.ToListAsync());
        Assert.Empty(await verifyDb.AudioRecordings.ToListAsync());
    }

    [Fact]
    public async Task AssessPronunciation_ExistingProgress_IsUpdatedInTheHandlerCommit()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);

        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true);
        db.AiModelCatalog.Add(model);

        var vocab = VocabularyEntity.Create("articulate");
        var sense = vocab.AddSense("verb", "Express clearly", "Diễn đạt rõ ràng", "B2");
        db.Vocabularies.Add(vocab);
        var progress = new UserVocabularyProgressEntity(
            userId: "learner-user-id",
            vocabularyId: vocab.Id,
            senseId: sense.Id);
        progress.UpdateLatestPronunciationScore(60m);
        db.UserVocabularyProgresses.Add(progress);

        var rubric = new ScoringRubricEntity("ENG_PRON_V1", "English Pronunciation Rubric", assessmentType: "Pronunciation");
        rubric.AddCriterion("ACCURACY", "Accuracy", "Pronunciation accuracy", weight: 100m);
        db.ScoringRubrics.Add(rubric);

        await db.SaveChangesAsync();

        var handler = new AssessPronunciationCommandHandler(
            db,
            countingUow,
            new FakePronunciationAiAssessor(),
            new AiModelCatalogReader(db),
            new PronunciationTargetTextSource(db),
            new VocabularyPronunciationProgressObserver(db));
        var cmd = new AssessPronunciationCommand("learner-user-id", new SubmitPronunciationAssessmentRequest(
            ApiKey: "test-gemini-key",
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            TargetType: "Word",
            AssessmentModelCode: "gemini-3.8-flash",
            Base64Audio: Convert.ToBase64String(new byte[1024]),
            ContentType: "audio/webm",
            DurationMilliseconds: 1500
        ));

        var result = await handler.HandleAsync(cmd);

        Assert.Equal(90m, result.OverallScore);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var savedProgress = await verifyDb.UserVocabularyProgresses.SingleAsync(
            p => p.UserId == "learner-user-id" && p.SenseId == sense.Id);
        Assert.Equal(90m, savedProgress.LatestPronunciationScore);
    }

    [Fact]
    public async Task AssessPronunciation_ProviderThrows_DoesNotUpdateExistingProgress()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);

        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true);
        db.AiModelCatalog.Add(model);

        var vocab = VocabularyEntity.Create("eloquent");
        var sense = vocab.AddSense("adjective", "Fluent and persuasive", "Lưu loát", "B2");
        db.Vocabularies.Add(vocab);
        var progress = new UserVocabularyProgressEntity(
            userId: "learner-user-id",
            vocabularyId: vocab.Id,
            senseId: sense.Id);
        progress.UpdateLatestPronunciationScore(42m);
        db.UserVocabularyProgresses.Add(progress);

        var rubric = new ScoringRubricEntity("ENG_PRON_V1", "English Pronunciation Rubric", assessmentType: "Pronunciation");
        rubric.AddCriterion("ACCURACY", "Accuracy", "Pronunciation accuracy", weight: 100m);
        db.ScoringRubrics.Add(rubric);

        await db.SaveChangesAsync();

        var handler = new AssessPronunciationCommandHandler(
            db,
            countingUow,
            new FakePronunciationAiAssessor(() => throw new HttpRequestException("Gemini service unavailable")),
            new AiModelCatalogReader(db),
            new PronunciationTargetTextSource(db),
            new VocabularyPronunciationProgressObserver(db));
        var cmd = new AssessPronunciationCommand("learner-user-id", new SubmitPronunciationAssessmentRequest(
            ApiKey: "test-gemini-key",
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            TargetType: "Word",
            AssessmentModelCode: "gemini-3.8-flash",
            Base64Audio: Convert.ToBase64String(new byte[1024]),
            ContentType: "audio/webm",
            DurationMilliseconds: 1500
        ));

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(cmd));

        Assert.Equal(0, countingUow.CommitCount);
        await using var verifyDb = new AppDbContext(options);
        var savedProgress = await verifyDb.UserVocabularyProgresses.SingleAsync(
            p => p.UserId == "learner-user-id" && p.SenseId == sense.Id);
        Assert.Equal(42m, savedProgress.LatestPronunciationScore);
    }

    #endregion

    #region 3. GenerateVocabulary Tests

    [Fact]
    public async Task GenerateVocabulary_Success_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var dbFactory = new InMemoryDbFactory(options);

        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true);
        db.AiModelCatalog.Add(model);
        await db.SaveChangesAsync();

        var generator = new FakeVocabularyAiGenerator();
        var promptBuilder = new VocabularyPromptBuilder(
            new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(dbFactory),
            new VocabularyGenerationContent());
        var writer = new VocabularyGenerationWriter(db, countingUow, new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(dbFactory), new DailyVocabularyPlanInitializer(db, new UserDirectory(db), new PronunciationScoreReader(db)), new PronunciationScoreReader(db), NullLogger<VocabularyGenerationWriter>.Instance);

        var handler = new GenerateVocabularyCommandHandler(
            generator,
            promptBuilder,
            writer,
            new AiModelCatalogReader(db),
            TestAgyCliRunner.Disabled);
        var cmd = new GenerateVocabularyCommand("learner-user-1", new GenerateVocabularyRequest(
            ApiKey: "valid-gemini-key",
            ModelCode: "gemini-3.8-flash",
            CefrLevel: "B2",
            Topic: "Technology",
            RequestedCount: 1,
            AssignToTodayPlan: true));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var vocabInDb = await verifyDb.Vocabularies.Include(v => v.Senses).FirstOrDefaultAsync(v => v.Word == "innovative");
        Assert.NotNull(vocabInDb);
        Assert.Single(vocabInDb.Senses);

        var auditInDb = await verifyDb.VocabularyGenerationAudits.FirstOrDefaultAsync(a => a.UserId == "learner-user-1");
        Assert.NotNull(auditInDb);
        Assert.Equal("Completed", auditInDb.Status);
        Assert.Equal(1, auditInDb.AcceptedCount);
    }

    [Fact]
    public async Task GenerateVocabulary_ProviderThrowsWithApiKey_DoesNotSaveVocabulary_SavesSanitizedAuditWithoutSecret()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var dbFactory = new InMemoryDbFactory(options);

        var model = new AiModelConfigEntity(
            displayName: "Gemini 3.8 Flash",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true);
        db.AiModelCatalog.Add(model);
        await db.SaveChangesAsync();

        string leakedSecret = "AIzaSySecretApiKey123456789";
        var failingGenerator = new FakeVocabularyAiGenerator(() =>
            throw new HttpRequestException($"Provider failed connecting to https://generativelanguage.googleapis.com/v1beta/models/gemini?key={leakedSecret}&alt=json (403 Forbidden)"));

        var promptBuilder = new VocabularyPromptBuilder(
            new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(dbFactory),
            new VocabularyGenerationContent());
        var writer = new VocabularyGenerationWriter(db, countingUow, new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(dbFactory), new DailyVocabularyPlanInitializer(db, new UserDirectory(db), new PronunciationScoreReader(db)), new PronunciationScoreReader(db), NullLogger<VocabularyGenerationWriter>.Instance);

        var handler = new GenerateVocabularyCommandHandler(
            failingGenerator,
            promptBuilder,
            writer,
            new AiModelCatalogReader(db),
            TestAgyCliRunner.Disabled);
        var cmd = new GenerateVocabularyCommand("learner-user-2", new GenerateVocabularyRequest(
            ApiKey: leakedSecret,
            ModelCode: "gemini-3.8-flash",
            CefrLevel: "B2",
            Topic: "Science",
            RequestedCount: 1));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(cmd));

        // The scoped UnitOfWork was never committed
        Assert.Equal(0, countingUow.CommitCount);

        // Check database with a new context
        await using var verifyDb = new AppDbContext(options);

        // No vocabulary entities must be saved
        Assert.Empty(await verifyDb.Vocabularies.ToListAsync());
        Assert.Empty(await verifyDb.VocabularySenses.ToListAsync());

        // Failed audit record MUST be recorded via isolated factory context
        var audits = await verifyDb.VocabularyGenerationAudits.ToListAsync();
        var audit = Assert.Single(audits);
        Assert.Equal("Failed", audit.Status);
        Assert.Equal("learner-user-2", audit.UserId);

        // SECURITY: audit.ErrorMessage must contain exception type and sanitized message, NOT the secret
        Assert.NotNull(audit.ErrorMessage);
        Assert.Contains(nameof(HttpRequestException), audit.ErrorMessage);
        Assert.DoesNotContain(leakedSecret, audit.ErrorMessage);
        Assert.Contains("key=[REDACTED]", audit.ErrorMessage);
    }

    #endregion

    #region 4. ImportVocabularyJson Tests

    [Fact]
    public async Task ImportVocabularyJson_SavesFullGraphAndAssignsToDailyPlan_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var dbFactory = new InMemoryDbFactory(options);

        // Import only fills an existing plan; InitializeDailyVocabularyPlan owns plan creation.
        string today = DailyVocabularyPlanEntity.ResolveLocalLearningDate("Asia/Ho_Chi_Minh");
        await using (var seedDb = new AppDbContext(options))
        {
            seedDb.DailyVocabularyPlans.Add(new DailyVocabularyPlanEntity("learner-user-3", today, newVocabularyTarget: 10));
            await seedDb.SaveChangesAsync();
        }
        var writer = new VocabularyGenerationWriter(db, countingUow, new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(dbFactory), new DailyVocabularyPlanInitializer(db, new UserDirectory(db), new PronunciationScoreReader(db)), new PronunciationScoreReader(db), NullLogger<VocabularyGenerationWriter>.Instance);
        var handler = new ImportVocabularyJsonCommandHandler(new VocabularyGenerationContent(), writer);

        string sampleJson = """
            [{
              "word": "adaptable", "partOfSpeech": "adjective", "definitionEn": "Able to adjust to new conditions",
              "meaningVi": "Dễ thích nghi", "descriptionEn": "Positive trait", "ipaUs": "/əˈdæp.tə.bəl/", "ipaUk": "/əˈdæp.tə.bəl/",
              "synonyms": ["flexible"], "antonyms": ["rigid"],
              "wordFamilies": [{"relatedWord": "adapt", "partOfSpeech": "verb"}],
              "collocations": [{"phraseEn": "adaptable workforce", "meaningVi": "lực lượng lao động thích ứng tốt"}],
              "examples": [{"sentenceEn": "She is very adaptable.", "sentenceVi": "Cô ấy rất dễ thích nghi."}],
              "grammarPatterns": [{"pattern": "adaptable to", "explanation": "adaptable + preposition", "example": "Adaptable to change."}]
            }]
            """;

        var cmd = new ImportVocabularyJsonCommand("learner-user-3", new ImportVocabularyJsonRequest(
            JsonText: sampleJson,
            ModelCode: "custom-import-model",
            CefrLevel: "B2",
            Topic: "General",
            AssignToTodayPlan: true));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        var card = Assert.Single(result);
        Assert.Equal("adaptable", card.Word);
        Assert.Equal(1, countingUow.CommitCount);

        // Verify full graph saved
        await using var verifyDb = new AppDbContext(options);
        var vocab = await verifyDb.Vocabularies.Include(v => v.Senses).FirstOrDefaultAsync(v => v.Word == "adaptable");
        Assert.NotNull(vocab);
        Assert.Single(vocab.Senses);

        // Verify today's plan created and item added
        var plan = await verifyDb.DailyVocabularyPlans.Include(p => p.Items).FirstOrDefaultAsync(p => p.UserId == "learner-user-3");
        Assert.NotNull(plan);
        Assert.Single(plan.Items);
        Assert.Equal(card.SenseId, plan.Items.First().SenseId);

        // Verify audit completed
        var audit = await verifyDb.VocabularyGenerationAudits.FirstOrDefaultAsync(a => a.UserId == "learner-user-3");
        Assert.NotNull(audit);
        Assert.Equal("Completed", audit.Status);
        Assert.Equal(1, audit.AcceptedCount);
    }

    #endregion

    [Theory]
    [InlineData("POST https://example/models/m:generateContent?key=AIzaSyFAKEFAKEFAKEFAKEFAKEFAKEFAKE12 failed")]
    [InlineData("url ...generateContent?key%3DTOPSECRETVALUE failed")]
    [InlineData("request header x-goog-api-key: TOPSECRETVALUE rejected")]
    [InlineData("{\"error\":\"bad\",\"key\":\"TOPSECRETVALUE\"}")]
    [InlineData("Authorization: Bearer TOPSECRETVALUE.part2")]
    [InlineData("bare AIzaSyFAKEFAKEFAKEFAKEFAKEFAKEFAKE12 in message")]
    public void SanitizeErrorMessage_RemovesKeys(string message)
    {
        string sanitized = VocabularyGenerationWriter.SanitizeErrorMessage(new InvalidOperationException(message));

        Assert.StartsWith("InvalidOperationException: ", sanitized);
        Assert.DoesNotContain("TOPSECRETVALUE", sanitized);
        Assert.DoesNotContain("AIzaSyFAKE", sanitized);
    }
}
