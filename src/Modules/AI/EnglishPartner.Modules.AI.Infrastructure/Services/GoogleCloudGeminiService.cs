using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Settings;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Alternative implementation of IGeminiService using the official Google Cloud AIPlatform V1 SDK.
/// This connects to Vertex AI via gRPC and Application Default Credentials.
/// 
/// Note: Requires 'ProjectId' and 'Location' (e.g. us-central1) in GeminiAISettings.
/// To use this, change DependencyInjection to register this instead of GeminiService/MsccGeminiService.
/// </summary>
internal sealed class GoogleCloudGeminiService(
    IOptions<GeminiAISettings> settings,
    AiDbContext dbContext,
    ILogger<GoogleCloudGeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private GeminiAISettings Config => settings.Value;

    // ── Client ───────────────────────────────────────────────────────────────

    private async Task<PredictionServiceClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Config.ProjectId))
            throw new InvalidOperationException("ProjectId must be configured in GeminiAISettings to use the Google Cloud AIPlatform SDK.");

        return await new PredictionServiceClientBuilder
        {
            Endpoint = $"{Config.Location}-aiplatform.googleapis.com"
        }.BuildAsync(cancellationToken);
    }

    private string BuildModelName() =>
        $"projects/{Config.ProjectId}/locations/{Config.Location}/publishers/google/models/{Config.ModelName}";

    private GenerationConfig BuildGenerationConfig(bool requireJsonResponse = true)
    {
        var cfg = new GenerationConfig
        {
            MaxOutputTokens = Config.MaxOutputTokens,
            Temperature = (float)Config.Temperature,
            TopP = (float)Config.TopP
        };

        if (requireJsonResponse)
            cfg.ResponseMimeType = "application/json";

        return cfg;
    }

    private IEnumerable<SafetySetting> BuildSafetySettings()
    {
        if (!Config.DisableAllSafetyRestrictions) return Enumerable.Empty<SafetySetting>();

        return
        [
            new SafetySetting { Category = HarmCategory.HateSpeech, Threshold = SafetySetting.Types.HarmBlockThreshold.Off },
            new SafetySetting { Category = HarmCategory.DangerousContent, Threshold = SafetySetting.Types.HarmBlockThreshold.Off },
            new SafetySetting { Category = HarmCategory.SexuallyExplicit, Threshold = SafetySetting.Types.HarmBlockThreshold.Off },
            new SafetySetting { Category = HarmCategory.Harassment, Threshold = SafetySetting.Types.HarmBlockThreshold.Off }
        ];
    }

    // ── Evaluation ───────────────────────────────────────────────────────────

    public async Task<EvaluationResult> EvaluateInputAsync(string itemContext, string userInput, CancellationToken cancellationToken = default)
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

        var (json, _, _, _) = await CallWithLoggingAsync("Evaluate", prompt, null, true, cancellationToken);
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
            logger.LogWarning(ex, "[GoogleCloudGemini] Failed to parse evaluation response: {Json}", json);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    public async Task<EvaluationResult> EvaluateAudioAsync(string itemContext, byte[] audioData, string mimeType, CancellationToken cancellationToken = default)
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

        var client = await CreateClientAsync(cancellationToken);
        var request = new GenerateContentRequest
        {
            Model = BuildModelName(),
            GenerationConfig = BuildGenerationConfig(requireJsonResponse: true),
        };
        request.SafetySettings.AddRange(BuildSafetySettings());

        var content = new Content { Role = "user" };
        content.Parts.Add(new Part { Text = prompt });
        content.Parts.Add(new Part
        {
            InlineData = new Blob
            {
                MimeType = mimeType,
                Data = Google.Protobuf.ByteString.CopyFrom(audioData)
            }
        });
        request.Contents.Add(content);

        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[GoogleCloudGemini] Calling EvaluateAudio on model {Model}", Config.ModelName);
            var response = await client.GenerateContentAsync(request, cancellationToken: cancellationToken);
            responseText = response.Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[GoogleCloudGemini] EvaluateAudio failed");
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
            logger.LogWarning(ex, "[GoogleCloudGemini] Failed to parse audio evaluation response: {Json}", responseText);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    // ── Chat ─────────────────────────────────────────────────────────────────

    public async Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be empty for ChatAsync.", nameof(userMessage));

        var client = await CreateClientAsync(cancellationToken);

        var request = new GenerateContentRequest
        {
            Model = BuildModelName(),
            GenerationConfig = BuildGenerationConfig(requireJsonResponse: false),
            SystemInstruction = new Content
            {
                Role = "system",
                Parts = { new Part { Text = systemPrompt } }
            }
        };
        request.SafetySettings.AddRange(BuildSafetySettings());

        foreach (var h in history)
        {
            request.Contents.Add(new Content
            {
                Role = h.Role,
                Parts = { new Part { Text = h.Text } }
            });
        }

        var sw = Stopwatch.StartNew();
        string responseText = "";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            var response = await client.GenerateContentAsync(request, cancellationToken: cancellationToken);
            responseText = response.Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[GoogleCloudGemini] Chat failed");
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create("Chat", userMessage, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        logger.LogInformation("[GoogleCloudGemini] Chat completed in {Ms}ms (success={Success})", sw.ElapsedMilliseconds, isSuccess);
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

    public async Task<List<GeminiGeneratedVocabulary>> GenerateVocabularyAsync(string targetLanguage, string wordHint, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var scope = count == 1
            ? $"Generate vocabulary data for the word \"{wordHint}\" in the language: {targetLanguage}."
            : $"Generate {count} vocabulary words related to the topic: \"{wordHint}\" in the language: {targetLanguage}. Pick diverse, useful words.";

        var prompt = $$"""
            {{SystemIntro}}
            
            {{scope}}
            
            Respond ONLY with this exact JSON array wrapped in an object:
            {
              "items": [
                {
                  "word": "<the word in {{targetLanguage}}>",
                  "definition": "<clear explanation or meaning>",
                  "pronunciation": "<pronunciation guide, e.g. 'per-se-VEER' or romaji/pinyin if applicable>",
                  "phoneticTranscription": "<IPA notation if applicable, e.g. /pəˌsɪˈvɪər/>",
                  "partOfSpeech": "<noun|verb|adjective|adverb|preposition|conjunction|interjection>",
                  "level": "<A1|A2|B1|B2|C1|C2>",
                  "synonyms": ["<synonym1>", "<synonym2>", "<synonym3>"]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateVocabulary", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<VocabularyJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(v => new GeminiGeneratedVocabulary(
                v.Word ?? wordHint, v.Definition ?? "", v.Pronunciation,
                v.PhoneticTranscription, v.PartOfSpeech, v.Level, v.Synonyms ?? [])).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[GoogleCloudGemini] Failed to parse vocabulary response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedVocabulary(wordHint, "", null, null, null, null, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedGrammar>> GenerateGrammarAsync(string targetLanguage, string title, string? level, int exampleCount, List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Assess the CEFR level yourself.";
        var scope = count == 1
            ? $"Generate a grammar lesson for: \"{title}\" for learning {targetLanguage}."
            : $"Generate {count} related grammar lessons, starting from \"{title}\" for learning {targetLanguage}.";
        var trans = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}
            
            {{scope}} {{levelHint}}
            Include exactly {{exampleCount}} example sentences per grammar rule.
            
            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<grammar rule title in {{targetLanguage}} or English>",
                  "explanation": "<clear explanation of the rule>",
                  "formula": "<structure formula if applicable>",
                  "level": "<A1|A2|B1|B2|C1|C2>",
                  "examples": [
                    {
                      "sentence": "<example sentence in {{targetLanguage}}>",
                      "note": "<short note explaining why this example illustrates the rule>",
                      {{trans}}
                    }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateGrammar", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<GrammarJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(g =>
            {
                var examples = (g.Examples ?? []).Select(e => new GeminiGrammarExample(e.Sentence ?? "", e.Note, e.Translations ?? [])).ToList();
                return new GeminiGeneratedGrammar(g.Title ?? title, g.Explanation, g.Formula, g.Level ?? level, examples);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[GoogleCloudGemini] Failed to parse grammar response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedGrammar(title, null, null, level, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedConversation>> GenerateFullConversationAsync(string targetLanguage, string topicName, string? level, string? scenario, int lineCount, List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 5);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Assess the appropriate level.";
        var scenarioHint = scenario is not null ? $"Scenario: {scenario}." : "";
        var trans = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}
            
            Generate {{count}} realistic conversation(s) in {{targetLanguage}} about the topic: "{{topicName}}".
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
                      "text": "<dialogue line in {{targetLanguage}}>",
                      {{trans}}
                    }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateConversation", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<FullConversationJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(c =>
            {
                var lines = (c.Lines ?? []).Select(l => new GeminiConversationLine(l.Speaker ?? "", l.Text ?? "", l.Translations ?? [])).ToList();
                return new GeminiGeneratedConversation(c.Title ?? topicName, lines);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[GoogleCloudGemini] Failed parse conversation: {Json}", json);
            return [];
        }
    }

    public async Task<List<GeminiGeneratedExercise>> GenerateExerciseAsync(string targetLanguage, string context, int exerciseType, string? level, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Determine the appropriate level.";

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} exercise(s) for learning {{targetLanguage}} of type {{exerciseType}} based on this context: "{{context}}".
            {{levelHint}}

            Exercise types: 1=MultipleChoice, 2=TrueFalse, 3=FillInTheBlank, 4=Translation, 5=Ordering, 6=Matching, 7=OpenEnded, 8=Pronunciation.

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<exercise title>",
                  "instruction": "<clear instruction for the student>",
                  "content": "<question or sentence with blank in {{targetLanguage}} or mixed>",
                  "correctAnswer": "<the correct answer>",
                  "explanation": "<brief explanation of why this is correct>",
                  "options": [
                    { "text": "<option text>", "isCorrect": <bool> }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateExercise", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExerciseJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(e =>
            {
                var options = (e.Options ?? []).Select(o => new GeminiExerciseOption(o.Text ?? "", o.IsCorrect)).ToList();
                return new GeminiGeneratedExercise(e.Title ?? "", e.Instruction ?? "", e.Content, e.CorrectAnswer ?? "", e.Explanation, options);
            }).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task<List<GeminiGeneratedExampleSentence>> GenerateExampleSentencesAsync(string targetLanguage, string context, int count, string? level, List<string>? targetLanguageCodes = null, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "";
        var trans = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}
            
            Generate {{count}} example sentence(s) in {{targetLanguage}} demonstrating: "{{context}}". {{levelHint}}
            
            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "text": "<example sentence in {{targetLanguage}}>",
                  "note": "<short note explaining how this illustrates the concept>",
                  {{trans}}
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateExampleSentences", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExampleSentenceJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(s => new GeminiGeneratedExampleSentence(s.Text ?? "", s.Note, s.Translations ?? [])).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }


    // ── Core execution ───────────────────────────────────────────────────────

    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> CallWithLoggingAsync(
        string requestType, string prompt, string? systemInstruction, bool requireJson, CancellationToken ct)
    {
        var client = await CreateClientAsync(ct);
        var request = new GenerateContentRequest
        {
            Model = BuildModelName(),
            GenerationConfig = BuildGenerationConfig(requireJson)
        };
        request.SafetySettings.AddRange(BuildSafetySettings());

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            request.SystemInstruction = new Content { Role = "system", Parts = { new Part { Text = systemInstruction } } };
        }

        request.Contents.Add(new Content { Role = "user", Parts = { new Part { Text = prompt } } });

        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[GoogleCloudGemini] Calling {RequestType} on model {Model}", requestType, Config.ModelName);
            var response = await client.GenerateContentAsync(request, cancellationToken: ct);
            responseText = response.Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? (requireJson ? "{}" : "");
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = requireJson ? "{}" : "";
            logger.LogError(ex, "[GoogleCloudGemini] {RequestType} failed", requestType);
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create(requestType, prompt, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        logger.LogInformation("[GoogleCloudGemini] {RequestType} completed in {Ms}ms (success={Success})", requestType, sw.ElapsedMilliseconds, isSuccess);

        return (responseText, sw.ElapsedMilliseconds, isSuccess, errorMsg);
    }

    // ── Internal JSON DTOs ───────────────────────────────────────────────────

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
