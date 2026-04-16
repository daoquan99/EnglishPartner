using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Alternative implementation of IGeminiService using Mscc.GenerativeAI SDK.
/// Replaces raw HttpClient + manual JSON with the official community Gemini SDK.
///
/// To switch: change DependencyInjection.cs to register this instead of GeminiService.
/// The original GeminiService.cs remains untouched as fallback.
/// </summary>
internal sealed class MsccGeminiService(
    IOptions<GeminiAISettings> settings,
    AiDbContext dbContext,
    ILogger<MsccGeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private GeminiAISettings Config => settings.Value;

    // ── Client factory ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a configured GoogleAI client using the API key from GeminiAISettings.
    /// Mscc.GenerativeAI targets Vertex AI Express Mode via API key — same as our REST implementation.
    /// </summary>
    private GenerativeModel CreateModel()
    {
        var google = new GoogleAI(Config.ApiKey);
        var model = google.GenerativeModel(
            model: Config.ModelName,
            generationConfig: BuildGenerationConfig(),
            safetySettings: BuildSafetySettings());
        return model;
    }

    // ── Evaluation ───────────────────────────────────────────────────────────

    public async Task<EvaluationResult> EvaluateInputAsync(
        string itemContext, string userInput, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are an English language teacher evaluating a student's answer.

            Context (question/prompt the student is answering):
            {{itemContext}}

            Student's answer:
            {{userInput}}

            Evaluate the answer and respond in this exact JSON format (no markdown):
            {
              "qualityScore": <int 0-5 where 0=blackout/no answer, 1=completely wrong, 2=partially wrong, 3=correct but with mistakes, 4=correct with minor issues, 5=perfect>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong>"
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("Evaluate", prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(json, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            return new EvaluationResult(
                Math.Clamp(parsed.QualityScore, 0, 5),
                parsed.IsCorrect,
                parsed.SuggestedCorrection ?? "",
                parsed.Explanation ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse evaluation response: {Json}", json);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    public async Task<EvaluationResult> EvaluateAudioAsync(
        string itemContext, byte[] audioData, string mimeType, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are an English language teacher evaluating a student's spoken audio answer.

            Context (question/prompt the student is answering):
            {{itemContext}}

            First, carefully transcribe what the student said in the audio.
            Then, evaluate the answer and respond in this exact JSON format (no markdown):
            {
              "transcription": "<exact transcription of the student's audio>",
              "qualityScore": <int 0-5>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer if they made a mistake>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong>"
            }
            """;

        var model = CreateModel();
        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            // Mscc.GenerativeAI: pass inline data as Part alongside text
            var parts = new List<IPart>
            {
                new TextData { Text = prompt },
                new InlineData { MimeType = mimeType, Data = Convert.ToBase64String(audioData) }
            };

            var response = await model.GenerateContent(parts);
            responseText = response.Text ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[MsccGemini] EvaluateAudio failed");
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create("EvaluateAudio", prompt, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(responseText, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            return new EvaluationResult(
                Math.Clamp(parsed.QualityScore, 0, 5),
                parsed.IsCorrect,
                parsed.SuggestedCorrection ?? "",
                parsed.Explanation ?? "",
                parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse audio evaluation response: {Json}", responseText);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    // ── Chat ─────────────────────────────────────────────────────────────────
    public async Task<string> ChatAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage,
        CancellationToken cancellationToken = default)
    {
        var model = CreateModel();
        var sw = Stopwatch.StartNew();
        string responseText = "";
        bool isSuccess = true;
        string? errorMsg = null;

        _ = model; // suppress unused-variable warning — model is still referenced by EvaluateAudio pattern; here we create fresh client inside try

        try
        {
            var request = new GenerateContentRequest
            {
                SystemInstruction = new Mscc.GenerativeAI.Types.Content
                {
                    Parts = [new Mscc.GenerativeAI.Types.TextData { Text = systemPrompt }]
                },
                Contents = history.Select(h => new Mscc.GenerativeAI.Types.Content
                {
                    Role = h.Role,
                    Parts = [new Mscc.GenerativeAI.Types.TextData { Text = h.Text }]
                }).ToList(),
                GenerationConfig = BuildGenerationConfig(requireJsonResponse: false),
                SafetySettings = BuildSafetySettings()
            };



            var geminiModel = new GoogleAI(Config.ApiKey).GenerativeModel(model: Config.ModelName);
            var response = await geminiModel.GenerateContent(request, cancellationToken: cancellationToken);
            responseText = response.Text ?? "";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[MsccGemini] Chat failed");
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create("Chat", userMessage, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        logger.LogInformation("[MsccGemini] Chat completed in {Ms}ms (success={Success})", sw.ElapsedMilliseconds, isSuccess);
        return responseText;
    }

    // ── Content Generation ───────────────────────────────────────────────────

    private const string SystemIntro =
        "You are an AI assistant powering an English learning app. " +
        "Generate accurate, pedagogically sound, and learner-friendly educational content for English learners. " +
        "Follow the output format exactly. Output only valid JSON, no markdown.";

    private static string BuildTranslationsInstruction(List<string>? codes)
    {
        if (codes is null or { Count: 0 }) return "\"translations\": {}";
        var langs = string.Join(", ", codes.Select(c => $"\"{c}\": \"<translation in {c}>\""));
        return $"\"translations\": {{ {langs} }}";
    }

    public async Task<List<GeminiGeneratedVocabulary>> GenerateVocabularyAsync(
        string wordHint, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var scope = count == 1
            ? $"Generate vocabulary data for the word \"{wordHint}\"."
            : $"Generate {count} English vocabulary words related to the topic: \"{wordHint}\". Pick diverse, useful words.";

        var prompt = $$"""
            {{SystemIntro}}

            {{scope}}

            Respond ONLY with this exact JSON array wrapped in an object:
            {
              "items": [
                {
                  "word": "<the English word>",
                  "definition": "<clear English definition>",
                  "pronunciation": "<pronunciation guide, e.g. 'per-se-VEER'>",
                  "phoneticTranscription": "<IPA notation, e.g. /pəˌsɪˈvɪər/>",
                  "partOfSpeech": "<noun|verb|adjective|adverb|preposition|conjunction|interjection>",
                  "level": "<A1|A2|B1|B2|C1|C2>",
                  "synonyms": ["<synonym1>", "<synonym2>", "<synonym3>"]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateVocabulary", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<VocabularyJson>>(json, JsonOptions);
            return (wrapper?.Items ?? [])
                .Select(v => new GeminiGeneratedVocabulary(
                    v.Word ?? wordHint, v.Definition ?? "", v.Pronunciation,
                    v.PhoneticTranscription, v.PartOfSpeech, v.Level, v.Synonyms ?? []))
                .ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse vocabulary response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedVocabulary(wordHint, "", null, null, null, null, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedGrammar>> GenerateGrammarAsync(
        string title, string? level, int exampleCount, List<string>? targetLanguageCodes = null,
        int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Assess the CEFR level yourself.";
        var scope = count == 1
            ? $"Generate a grammar lesson for: \"{title}\"."
            : $"Generate {count} related grammar lessons, starting from \"{title}\".";
        var translationsInstruction = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}

            {{scope}} {{levelHint}}
            Include exactly {{exampleCount}} example sentences per grammar rule.

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<grammar rule title>",
                  "explanation": "<clear English explanation>",
                  "formula": "<structure formula, e.g. 'Subject + have/has + past participle'>",
                  "level": "<A1|A2|B1|B2|C1|C2>",
                  "examples": [
                    {
                      "sentence": "<English example sentence>",
                      "note": "<short note explaining why this example illustrates the rule>",
                      {{translationsInstruction}}
                    }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateGrammar", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<GrammarJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(g =>
            {
                var examples = (g.Examples ?? [])
                    .Select(e => new GeminiGrammarExample(e.Sentence ?? "", e.Note, e.Translations ?? []))
                    .ToList();
                return new GeminiGeneratedGrammar(g.Title ?? title, g.Explanation, g.Formula, g.Level ?? level, examples);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse grammar response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedGrammar(title, null, null, level, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedConversation>> GenerateFullConversationAsync(
        string topicName, string? level, string? scenario, int lineCount,
        List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 5);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Assess the appropriate level.";
        var scenarioHint = scenario is not null ? $"Scenario: {scenario}." : "";
        var translationsInstruction = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} realistic English conversation(s) about the topic: "{{topicName}}".
            {{scenarioHint}} {{levelHint}}
            Each conversation must have exactly {{lineCount}} lines alternating between 2 speakers.

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<conversation title>",
                  "lines": [
                    {
                      "speaker": "<Speaker A or Speaker B>",
                      "text": "<English dialogue line>",
                      {{translationsInstruction}}
                    }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateConversation", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<FullConversationJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(c =>
            {
                var lines = (c.Lines ?? [])
                    .Select(l => new GeminiConversationLine(l.Speaker ?? "", l.Text ?? "", l.Translations ?? []))
                    .ToList();
                return new GeminiGeneratedConversation(c.Title ?? topicName, lines);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse conversation response: {Json}", json);
            return [];
        }
    }

    public async Task<List<GeminiGeneratedExercise>> GenerateExerciseAsync(
        string context, int exerciseType, string? level, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Determine the appropriate level.";

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} English exercise(s) of type {{exerciseType}} based on this context: "{{context}}".
            {{levelHint}}

            Exercise types: 1=MultipleChoice, 2=TrueFalse, 3=FillInTheBlank, 4=Translation, 5=Ordering, 6=Matching, 7=OpenEnded, 8=Pronunciation.

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<exercise title>",
                  "instruction": "<clear instruction for the student>",
                  "content": "<question or sentence with blank>",
                  "correctAnswer": "<the correct answer>",
                  "explanation": "<brief explanation of why this is correct>",
                  "options": [
                    { "text": "<option text>", "isCorrect": <bool> }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateExercise", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExerciseJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(e =>
            {
                var options = (e.Options ?? [])
                    .Select(o => new GeminiExerciseOption(o.Text ?? "", o.IsCorrect))
                    .ToList();
                return new GeminiGeneratedExercise(
                    e.Title ?? "", e.Instruction ?? "", e.Content,
                    e.CorrectAnswer ?? "", e.Explanation, options);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse exercise response: {Json}", json);
            return [];
        }
    }

    public async Task<List<GeminiGeneratedExampleSentence>> GenerateExampleSentencesAsync(
        string context, int count, string? level, List<string>? targetLanguageCodes = null,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "";
        var translationsInstruction = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} example English sentence(s) demonstrating: "{{context}}". {{levelHint}}

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "text": "<English example sentence>",
                  "note": "<short note explaining how this illustrates the concept>",
                  {{translationsInstruction}}
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateExampleSentences", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExampleSentenceJson>>(json, JsonOptions);
            return (wrapper?.Items ?? [])
                .Select(s => new GeminiGeneratedExampleSentence(s.Text ?? "", s.Note, s.Translations ?? []))
                .ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[MsccGemini] Failed to parse example sentences response: {Json}", json);
            return [];
        }
    }

    // ── Core execution ───────────────────────────────────────────────────────

    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> CallWithLoggingAsync(
        string requestType, string prompt, CancellationToken cancellationToken)
    {
        var model = CreateModel();
        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[MsccGemini] Calling {RequestType} on model {Model}", requestType, Config.ModelName);
            var response = await model.GenerateContent(prompt);
            responseText = response.Text ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = "{}";
            logger.LogError(ex, "[MsccGemini] {RequestType} failed", requestType);
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create(requestType, prompt, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        logger.LogInformation("[MsccGemini] {RequestType} completed in {Ms}ms (success={Success})",
            requestType, sw.ElapsedMilliseconds, isSuccess);

        return (responseText, sw.ElapsedMilliseconds, isSuccess, errorMsg);
    }

    // ── Config builders ───────────────────────────────────────────────────────

    private Mscc.GenerativeAI.Types.GenerationConfig BuildGenerationConfig(bool requireJsonResponse = true)
    {
        var cfg = new Mscc.GenerativeAI.Types.GenerationConfig
        {
            MaxOutputTokens = Config.MaxOutputTokens,
            Temperature = (float)Config.Temperature,
            TopP = (float)Config.TopP
        };

        if (requireJsonResponse)
            cfg.ResponseMimeType = "application/json";

        return cfg;
    }

    private List<Mscc.GenerativeAI.Types.SafetySetting>? BuildSafetySettings()
    {
        if (!Config.DisableAllSafetyRestrictions) return null;

        return
        [
            new Mscc.GenerativeAI.Types.SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech,      Threshold = HarmBlockThreshold.Off },
            new Mscc.GenerativeAI.Types.SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.Off },
            new Mscc.GenerativeAI.Types.SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.Off },
            new Mscc.GenerativeAI.Types.SafetySetting { Category = HarmCategory.HarmCategoryHarassment,       Threshold = HarmBlockThreshold.Off }
        ];
    }

    // ── Internal JSON DTOs (same as GeminiService) ───────────────────────────

    private sealed class EvaluationJsonResponse
    {
        public int QualityScore { get; set; }
        public bool IsCorrect { get; set; }
        public string? SuggestedCorrection { get; set; }
        public string? Explanation { get; set; }
        public string? Transcription { get; set; }
    }

    private sealed class VocabularyJson
    {
        public string? Word { get; set; }
        public string? Definition { get; set; }
        public string? Pronunciation { get; set; }
        public string? PhoneticTranscription { get; set; }
        public string? PartOfSpeech { get; set; }
        public string? Level { get; set; }
        public List<string>? Synonyms { get; set; }
    }

    private sealed class GrammarJson
    {
        public string? Title { get; set; }
        public string? Explanation { get; set; }
        public string? Formula { get; set; }
        public string? Level { get; set; }
        public List<GrammarExampleJson>? Examples { get; set; }
    }

    private sealed class GrammarExampleJson
    {
        public string? Sentence { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, string>? Translations { get; set; }
    }

    private sealed class FullConversationJson
    {
        public string? Title { get; set; }
        public List<FullConversationLineJson>? Lines { get; set; }
    }

    private sealed class FullConversationLineJson
    {
        public string? Speaker { get; set; }
        public string? Text { get; set; }
        public Dictionary<string, string>? Translations { get; set; }
    }

    private sealed class ExerciseJson
    {
        public string? Title { get; set; }
        public string? Instruction { get; set; }
        public string? Content { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public List<ExerciseOptionJson>? Options { get; set; }
    }

    private sealed class ExerciseOptionJson
    {
        public string? Text { get; set; }
        public bool IsCorrect { get; set; }
    }

    private sealed class ExampleSentenceJson
    {
        public string? Text { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, string>? Translations { get; set; }
    }

    private sealed class ItemsWrapper<T>
    {
        public List<T>? Items { get; set; }
    }
}
