using EnglishLiveTutor.Domain.Tests;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

public sealed class GeminiAdapterTelemetrySecurityTests
{
    private const string FakeApiKey = "TEST-KEY-123456";

    // =========================================================================
    // 1. Grammar (GeminiGrammarExerciseAiGenerator)
    // =========================================================================

    [Fact]
    public async Task GeminiGrammarExerciseAiGenerator_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""exercises\"":[]}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var generator = new GeminiGrammarExerciseAiGenerator(client, new FakeAiModelCatalogReader());

        var config = new GrammarAiExecutionConfig(ApiKey: FakeApiKey, ModelCode: "gemini-grammar-succ", Provider: "gemini");
        string result = await generator.GenerateRawAsync("generate exercises", config);

        Assert.Equal("{\"exercises\":[]}", result);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "grammar.generate", "gemini-grammar-succ");
    }

    [Fact]
    public async Task GeminiGrammarExerciseAiGenerator_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var generator = new GeminiGrammarExerciseAiGenerator(client, new FakeAiModelCatalogReader());

        var config = new GrammarAiExecutionConfig(ApiKey: FakeApiKey, ModelCode: "gemini-grammar-500", Provider: "gemini");
        var ex = await Record.ExceptionAsync(() => generator.GenerateRawAsync("generate exercises", config));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "grammar.generate", "gemini-grammar-500");
    }

    // =========================================================================
    // 2. LessonGenerator (GeminiLessonAiGenerator)
    // =========================================================================

    [Fact]
    public async Task GeminiLessonAiGenerator_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""exercises\"":[]}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonGenerationOptions { ApiKey = FakeApiKey, ModelCode = "gemini-lesson-succ", MaxRetries = 0 });
        var generator = new GeminiLessonAiGenerator(client, null, options, loggerProvider.CreateLogger<GeminiLessonAiGenerator>());

        string result = await generator.GenerateLessonExercisesJsonAsync("prompt", FakeApiKey, "gemini-lesson-succ", "gemini");

        Assert.Equal("{\"exercises\":[]}", result);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "lesson.generate", "gemini-lesson-succ");
    }

    [Fact]
    public async Task GeminiLessonAiGenerator_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonGenerationOptions { ApiKey = FakeApiKey, ModelCode = "gemini-lesson-500", MaxRetries = 0 });
        var generator = new GeminiLessonAiGenerator(client, null, options, loggerProvider.CreateLogger<GeminiLessonAiGenerator>());

        var ex = await Record.ExceptionAsync(() => generator.GenerateLessonExercisesJsonAsync("prompt", FakeApiKey, "gemini-lesson-500", "gemini"));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "lesson.generate", "gemini-lesson-500");
    }

    // =========================================================================
    // 3. RolePlay (GeminiRolePlayReplyProvider)
    // =========================================================================

    [Fact]
    public async Task GeminiRolePlayReplyProvider_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""Hello there!"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonRolePlayOptions { ApiKey = FakeApiKey, ModelCode = "gemini-roleplay-succ", MaxRetries = 0 });
        var provider = new GeminiRolePlayReplyProvider(client, options, loggerProvider.CreateLogger<GeminiRolePlayReplyProvider>());

        string reply = await provider.GenerateReplyAsync("scenario", "tutor", Array.Empty<RolePlayHistoryItem>(), 1, 3);

        Assert.Equal("Hello there!", reply);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "lesson.roleplay", "gemini-roleplay-succ");
    }

    [Fact]
    public async Task GeminiRolePlayReplyProvider_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonRolePlayOptions { ApiKey = FakeApiKey, ModelCode = "gemini-roleplay-500", MaxRetries = 0 });
        var provider = new GeminiRolePlayReplyProvider(client, options, loggerProvider.CreateLogger<GeminiRolePlayReplyProvider>());

        var ex = await Record.ExceptionAsync(() => provider.GenerateReplyAsync("scenario", "tutor", Array.Empty<RolePlayHistoryItem>(), 1, 3));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "lesson.roleplay", "gemini-roleplay-500");
    }

    // =========================================================================
    // 4. LessonAiGrader (LessonAiGrader)
    // =========================================================================

    [Fact]
    public async Task LessonAiGrader_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""isAssessable\"":true,\""criteria\"":[{\""code\"":\""Accuracy\"",\""rawScore\"":3,\""feedback\"":\""Tốt\""}],\""strengths\"":[\""Good\""],\""improvements\"":[\""None\""],\""suggestedAnswer\"":\""Hello\""}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonAssessmentOptions { ApiKey = FakeApiKey, ModelCode = "gemini-grader-succ", MaxRetries = 0 });
        var grader = new LessonAiGrader(client, new FakeAudioRecordingStore(), new FakeLessonMediaStorage(), new FakeAgyRunner(), options, loggerProvider.CreateLogger<LessonAiGrader>());

        var rubric = new ScoringRubricSnapshot(
            Code: "TEST_RUBRIC",
            Version: 1,
            Criteria: new[] { new ScoringRubricCriterionSnapshot("Accuracy", "Accuracy", "Desc", 100m, 1, true, "L0", "L1", "L2", "L3", "L4") });

        var aiConfig = new LessonAiExecutionConfigDto(ApiKey: FakeApiKey, AssessmentModelCode: "gemini-grader-succ", Provider: "gemini");
        var outcome = await grader.GradeSubmissionAsync(
            ExerciseType.WritingPractice,
            "Write a greeting",
            "A1",
            rubric,
            "{\"writtenText\":\"Hello world\"}",
            10m,
            aiConfig: aiConfig);

        Assert.True(outcome.IsAssessable);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "lesson.grade", "gemini-grader-succ");
    }

    [Fact]
    public async Task LessonAiGrader_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonAssessmentOptions { ApiKey = FakeApiKey, ModelCode = "gemini-grader-500", MaxRetries = 0 });
        var grader = new LessonAiGrader(client, new FakeAudioRecordingStore(), new FakeLessonMediaStorage(), new FakeAgyRunner(), options, loggerProvider.CreateLogger<LessonAiGrader>());

        var rubric = new ScoringRubricSnapshot(
            Code: "TEST_RUBRIC",
            Version: 1,
            Criteria: new[] { new ScoringRubricCriterionSnapshot("Accuracy", "Accuracy", "Desc", 100m, 1, true, "L0", "L1", "L2", "L3", "L4") });

        var aiConfig = new LessonAiExecutionConfigDto(ApiKey: FakeApiKey, AssessmentModelCode: "gemini-grader-500", Provider: "gemini");
        var ex = await Record.ExceptionAsync(() => grader.GradeSubmissionAsync(
            ExerciseType.WritingPractice,
            "Write a greeting",
            "A1",
            rubric,
            "{\"writtenText\":\"Hello world\"}",
            10m,
            aiConfig: aiConfig));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "lesson.grade", "gemini-grader-500");
    }

    // =========================================================================
    // 5. DictationAiReviewer (DictationAiReviewer)
    // =========================================================================

    [Fact]
    public async Task DictationAiReviewer_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""status\"":\""Completed\"",\""feedback\"":\""Tốt\"",\""missingWords\"":[],\""extraWords\"":[],\""wordSubstitutions\"":[],\""explanationsVi\"":\""Tốt\"",\""actionableImprovements\"":[]}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonAssessmentOptions { ApiKey = FakeApiKey, ModelCode = "gemini-dictation-succ", MaxRetries = 0 });
        var reviewer = new DictationAiReviewer(client, options, new FakeAgyRunner(), loggerProvider.CreateLogger<DictationAiReviewer>(), TimeProvider.System);

        var aiConfig = new LessonAiExecutionConfigDto(ApiKey: FakeApiKey, AssessmentModelCode: "gemini-dictation-succ", Provider: "gemini");
        var outcome = await reviewer.ReviewAsync(Guid.NewGuid(), "Hello world", "Hello world", aiConfig);

        Assert.Equal("Completed", outcome.Status);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "lesson.dictation_review", "gemini-dictation-succ");
    }

    [Fact]
    public async Task DictationAiReviewer_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var options = Options.Create(new LessonAssessmentOptions { ApiKey = FakeApiKey, ModelCode = "gemini-dictation-500", MaxRetries = 0 });
        var reviewer = new DictationAiReviewer(client, options, new FakeAgyRunner(), loggerProvider.CreateLogger<DictationAiReviewer>(), TimeProvider.System);

        var aiConfig = new LessonAiExecutionConfigDto(ApiKey: FakeApiKey, AssessmentModelCode: "gemini-dictation-500", Provider: "gemini");
        var ex = await Record.ExceptionAsync(() => reviewer.ReviewAsync(Guid.NewGuid(), "Hello world", "Hello world", aiConfig));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "lesson.dictation_review", "gemini-dictation-500");
    }

    // =========================================================================
    // 6. Translator (FastTextTranslatorFallback)
    // =========================================================================

    [Fact]
    public async Task FastTextTranslatorFallback_Success_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri != null && req.RequestUri.Host != "generativelanguage.googleapis.com")
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            if (req.Method == HttpMethod.Get)
            {
                string modelsJson = @"{ ""models"": [ { ""name"": ""models/gemini-translate-succ"", ""supportedGenerationMethods"": [""generateContent""] } ] }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(modelsJson, Encoding.UTF8, "application/json")
                };
            }

            string translateJson = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""Bầu trời trong xanh"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 5, ""candidatesTokenCount"": 10, ""totalTokenCount"": 15 }
}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(translateJson, Encoding.UTF8, "application/json")
            };
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);

        string? result = await translator.TranslateToVietnameseAsync(FakeApiKey, "The sky is blue today.");

        Assert.Equal("Bầu trời trong xanh", result);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "livetutor.translate", "gemini-translate-succ");
    }

    [Fact]
    public async Task FastTextTranslatorFallback_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri != null && req.RequestUri.Host != "generativelanguage.googleapis.com")
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            if (req.Method == HttpMethod.Get)
            {
                string modelsJson = @"{ ""models"": [ { ""name"": ""models/gemini-translate-500"", ""supportedGenerationMethods"": [""generateContent""] } ] }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(modelsJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
            };
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);

        string? result = await translator.TranslateToVietnameseAsync(FakeApiKey, "The sky is blue today.");

        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetryHttp500(measurements, "livetutor.translate", "gemini-translate-500");
    }

    // =========================================================================
    // 7. Correction (GrammarCorrectionAnalyzerService)
    // =========================================================================

    [Fact]
    public async Task GrammarCorrectionAnalyzerService_Success_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                string modelsJson = @"{ ""models"": [ { ""name"": ""models/gemini-correct-succ"", ""supportedGenerationMethods"": [""generateContent""] } ] }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(modelsJson, Encoding.UTF8, "application/json")
                };
            }

            string correctionJson = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""originalText\"":\""I go yesterday\"",\""correctedText\"":\""I went yesterday\"",\""errorCategory\"":\""Grammar\"",\""explanationVi\"":\""Chia quá khứ\"",\""cefrTip\"":\""Luyện động từ\""}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(correctionJson, Encoding.UTF8, "application/json")
            };
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var options = Options.Create(new LiveTutorAiOptions { CorrectionModelCode = "gemini-correct-succ" });
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator, options);

        var outcome = await analyzer.AnalyzeUserUtteranceAsync(FakeApiKey, "conv1", "turn1", "I go yesterday to the market", "A2");

        Assert.NotNull(outcome);
        Assert.Equal("I went yesterday", outcome.CorrectedText);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "livetutor.correction", "gemini-correct-succ");
    }

    [Fact]
    public async Task GrammarCorrectionAnalyzerService_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                string modelsJson = @"{ ""models"": [ { ""name"": ""models/gemini-correct-500"", ""supportedGenerationMethods"": [""generateContent""] } ] }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(modelsJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
            };
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var options = Options.Create(new LiveTutorAiOptions { CorrectionModelCode = "gemini-correct-500" });
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator, options);

        var outcome = await analyzer.AnalyzeUserUtteranceAsync(FakeApiKey, "conv1", "turn1", "I go yesterday to the market", "A2");

        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetryHttp500(measurements, "livetutor.correction", "gemini-correct-500");
    }

    // =========================================================================
    // 8. Pronunciation (GeminiPronunciationAiAssessor)
    // =========================================================================

    [Fact]
    public async Task GeminiPronunciationAiAssessor_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""{\""targetType\"":\""Word\"",\""expectedText\"":\""hello\"",\""recognizedText\"":\""hello\"",\""accent\"":\""US\"",\""isAssessable\"":true,\""confidence\"":0.95,\""criteria\"":[{\""code\"":\""Accuracy\"",\""score\"":90,\""feedback\"":\""Good\""}],\""strengths\"":[\""Good\""],\""improvements\"":[\""None\""],\""suggestedPractice\"":[\""None\""]}"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var assessor = new GeminiPronunciationAiAssessor(client);

        var rubric = new ScoringRubricEntity("PRON_RUBRIC", "Pronunciation Rubric");
        rubric.AddCriterion("Accuracy", "Accuracy", "Pronunciation accuracy", 100m);

        var outcome = await assessor.AssessAudioAsync(FakeApiKey, "gemini-pron-succ", "dGVzdA==", "audio/webm", "hello", "US", rubric);

        Assert.True(outcome.IsAssessable);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "speech.assess", "gemini-pron-succ");
    }

    [Fact]
    public async Task GeminiPronunciationAiAssessor_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var assessor = new GeminiPronunciationAiAssessor(client);

        var rubric = new ScoringRubricEntity("PRON_RUBRIC", "Pronunciation Rubric");
        rubric.AddCriterion("Accuracy", "Accuracy", "Pronunciation accuracy", 100m);

        var ex = await Record.ExceptionAsync(() => assessor.AssessAudioAsync(FakeApiKey, "gemini-pron-500", "dGVzdA==", "audio/webm", "hello", "US", rubric));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "speech.assess", "gemini-pron-500");
    }

    // =========================================================================
    // 9. Vocabulary (GeminiVocabularyAiGenerator)
    // =========================================================================

    [Fact]
    public async Task GeminiVocabularyAiGenerator_Success_DoesNotLeakKey()
    {
        string successBody = @"{
  ""candidates"": [{ ""content"": { ""parts"": [{ ""text"": ""[{\""word\"":\""apple\""}]"" }] } }],
  ""usageMetadata"": { ""promptTokenCount"": 10, ""candidatesTokenCount"": 20, ""totalTokenCount"": 30 }
}";
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(successBody, Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var generator = new GeminiVocabularyAiGenerator(client, new FakeVocabularyGenerationContent());

        var options = new VocabularyGenerationRequestOptions(ApiKey: FakeApiKey, ModelCode: "gemini-vocab-succ", CefrLevel: "A1", Topic: "Food", Count: 1, ExcludedWords: Array.Empty<string>(), Provider: "gemini");
        var result = await generator.GenerateVocabularyAsync(options);

        Assert.NotEmpty(result);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, null);
        AssertTelemetrySuccess(measurements, "vocabulary.generate", "gemini-vocab-succ");
    }

    [Fact]
    public async Task GeminiVocabularyAiGenerator_Http500_DoesNotLeakKey()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"{{\"error\":\"Server error with key {FakeApiKey}\"}}", Encoding.UTF8, "application/json")
        });

        using var telemetry = SetupTelemetryListeners();
        var (loggerProvider, activities, measurements) = telemetry;
        using var client = new HttpClient(handler);
        var generator = new GeminiVocabularyAiGenerator(client, new FakeVocabularyGenerationContent());

        var options = new VocabularyGenerationRequestOptions(ApiKey: FakeApiKey, ModelCode: "gemini-vocab-500", CefrLevel: "A1", Topic: "Food", Count: 1, ExcludedWords: Array.Empty<string>(), Provider: "gemini");
        var ex = await Record.ExceptionAsync(() => generator.GenerateVocabularyAsync(options));

        Assert.NotNull(ex);
        AssertNoApiKeyLeak(handler, loggerProvider, activities, measurements, ex);
        AssertTelemetryHttp500(measurements, "vocabulary.generate", "gemini-vocab-500");
    }

    // =========================================================================
    // Helper setup and assertion methods
    // =========================================================================

    private sealed class TelemetryTestScope : IDisposable
    {
        public TestLoggerProvider Logger { get; }
        public List<Activity> Activities { get; }
        public List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> Measurements { get; }
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;

        public TelemetryTestScope(
            TestLoggerProvider logger,
            List<Activity> activities,
            List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements,
            ActivityListener activityListener,
            MeterListener meterListener)
        {
            Logger = logger;
            Activities = activities;
            Measurements = measurements;
            _activityListener = activityListener;
            _meterListener = meterListener;
        }

        public void Deconstruct(
            out TestLoggerProvider logger,
            out List<Activity> activities,
            out List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements)
        {
            logger = Logger;
            activities = Activities;
            measurements = Measurements;
        }

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
            Logger.Dispose();
        }
    }

    private static TelemetryTestScope SetupTelemetryListeners()
    {
        var activities = new List<Activity>();
        var activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == AiTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (activities) activities.Add(a); }
        };
        ActivitySource.AddActivityListener(activityListener);

        var measurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (measurements) measurements.Add((instrument.Name, val, tags.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<long>((instrument, val, tags, state) =>
        {
            lock (measurements) measurements.Add((instrument.Name, val, tags.ToArray()));
        });
        meterListener.Start();

        var loggerProvider = new TestLoggerProvider();
        return new TelemetryTestScope(loggerProvider, activities, measurements, activityListener, meterListener);
    }

    private static void AssertTelemetrySuccess(
        List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements,
        string expectedFeature,
        string expectedModel,
        bool expectUsage = true)
    {
        string normalizedModel = AiTelemetry.NormalizeModelCode(expectedModel);
        var durationM = measurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == expectedFeature)
                              && m.Tags.Any(t => t.Key == "gen_ai.request.model" && (string?)t.Value == normalizedModel));

        Assert.NotNull(durationM.Name);
        Assert.True((double)durationM.Value > 0);
        Assert.DoesNotContain(durationM.Tags, t => t.Key == "error.type");

        if (expectUsage)
        {
            var tokenMeasurements = measurements
                .Where(m => m.Name == "gen_ai.client.token.usage" &&
                            m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == expectedFeature) &&
                            m.Tags.Any(t => t.Key == "gen_ai.request.model" && (string?)t.Value == normalizedModel))
                .ToList();

            Assert.Contains(tokenMeasurements, m => m.Tags.Any(t => t.Key == "gen_ai.token.type" && (string?)t.Value == "input") && (long)m.Value > 0);
            Assert.Contains(tokenMeasurements, m => m.Tags.Any(t => t.Key == "gen_ai.token.type" && (string?)t.Value == "output") && (long)m.Value > 0);
        }
    }

    private static void AssertTelemetryHttp500(
        List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements,
        string expectedFeature,
        string expectedModel)
    {
        string normalizedModel = AiTelemetry.NormalizeModelCode(expectedModel);
        var durationM = measurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == expectedFeature)
                              && m.Tags.Any(t => t.Key == "gen_ai.request.model" && (string?)t.Value == normalizedModel));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == "http_500");
    }

    private static void AssertNoApiKeyLeak(
        FakeHttpMessageHandler handler,
        TestLoggerProvider loggerProvider,
        List<Activity> activities,
        List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements,
        Exception? caughtException)
    {
        // 1. Captured Requests: URI must not contain FakeApiKey or ?key=; header x-goog-api-key must contain FakeApiKey for Gemini endpoints
        Assert.NotEmpty(handler.CapturedRequests);
        bool hasGeminiRequest = false;
        foreach (var req in handler.CapturedRequests)
        {
            string uri = req.RequestUri?.ToString() ?? string.Empty;
            Assert.DoesNotContain(FakeApiKey, uri, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("?key=", uri, StringComparison.OrdinalIgnoreCase);

            if (req.RequestUri?.Host == "generativelanguage.googleapis.com")
            {
                hasGeminiRequest = true;
                Assert.True(req.Headers.TryGetValues("x-goog-api-key", out var headerVals));
                Assert.Contains(FakeApiKey, headerVals);
            }
        }
        Assert.True(hasGeminiRequest, "At least one request must target Google AI Studio (generativelanguage.googleapis.com)");

        // 2. Exception: message and ToString must not contain FakeApiKey
        if (caughtException != null)
        {
            Assert.DoesNotContain(FakeApiKey, caughtException.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(FakeApiKey, caughtException.Message, StringComparison.OrdinalIgnoreCase);
        }

        // 3. Logger: messages and exception strings must not contain FakeApiKey
        foreach (var log in loggerProvider.LogEntries)
        {
            Assert.DoesNotContain(FakeApiKey, log, StringComparison.OrdinalIgnoreCase);
        }

        // 4. Activities: tags and values must not contain FakeApiKey
        foreach (var act in activities)
        {
            foreach (var tag in act.Tags)
            {
                Assert.DoesNotContain(FakeApiKey, tag.Key, StringComparison.OrdinalIgnoreCase);
                if (tag.Value != null)
                {
                    Assert.DoesNotContain(FakeApiKey, tag.Value, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // 5. Measurements: tags and values must not contain FakeApiKey
        foreach (var m in measurements)
        {
            if (m.Value is string sVal)
            {
                Assert.DoesNotContain(FakeApiKey, sVal, StringComparison.OrdinalIgnoreCase);
            }
            foreach (var tag in m.Tags)
            {
                Assert.DoesNotContain(FakeApiKey, tag.Key, StringComparison.OrdinalIgnoreCase);
                if (tag.Value is string strVal)
                {
                    Assert.DoesNotContain(FakeApiKey, strVal, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    // =========================================================================
    // Fakes
    // =========================================================================

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> CapturedRequests { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (CapturedRequests)
            {
                CapturedRequests.Add(request);
            }
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class TestLoggerProvider : ILoggerProvider, ILogger
    {
        public List<string> LogEntries { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public ILogger<T> CreateLogger<T>() => new TypedLogger<T>(this);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (LogEntries)
            {
                string msg = formatter(state, exception);
                if (!string.IsNullOrEmpty(msg))
                    LogEntries.Add(msg);
                if (exception != null)
                    LogEntries.Add(exception.ToString());
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Dispose() { }

        private sealed class TypedLogger<T> : ILogger<T>
        {
            private readonly TestLoggerProvider _provider;
            public TypedLogger(TestLoggerProvider provider) => _provider = provider;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => _provider.Log(logLevel, eventId, state, exception, formatter);
            public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider.BeginScope(state);
        }
    }

    private sealed class FakeAiModelCatalogReader : IAiModelCatalogReader
    {
        public Task<AiModelInfo?> FindByModelCodeAsync(string modelCode, CancellationToken ct = default) => Task.FromResult<AiModelInfo?>(null);
    }

    private sealed class FakeAgyRunner : IAgyCliRunner
    {
        public bool IsEnabled => true;
        public IReadOnlyCollection<string> AllowedModels => Array.Empty<string>();
        public bool IsAgyModel(string? model) => false;
        public string NormalizeModel(string? model) => "other";
        public Task<string> ExecutePromptAsync(string prompt, string? modelCode = null, CancellationToken ct = default) => Task.FromResult("{}");
    }

    private sealed class FakeVocabularyGenerationContent : IVocabularyGenerationContent
    {
        public string BuildPrompt(VocabularyPromptOptions options) => "Fake prompt";
        public List<GeneratedVocabularyRawSense> ParseResponse(string jsonText, string cefrLevel, int examplesPerSense = 5, IReadOnlyList<string>? targetTerms = null)
        {
            return new List<GeneratedVocabularyRawSense>
            {
                new GeneratedVocabularyRawSense(
                    Word: "apple",
                    PartOfSpeech: "noun",
                    CefrLevel: "A1",
                    DefinitionEn: "A fruit",
                    MeaningVi: "Quả táo",
                    DescriptionEn: null,
                    IpaUs: "/ˈæp.əl/",
                    IpaUk: null,
                    Synonyms: new(),
                    Antonyms: new(),
                    WordFamilies: new(),
                    Collocations: new(),
                    Examples: new() { new GeneratedExampleRaw("An apple", "Một quả táo") },
                    GrammarPatterns: new())
            };
        }
    }
}
