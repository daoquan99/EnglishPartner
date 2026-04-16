using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Communicates with Google Gemini API via REST.
/// Audit logs are added to DbContext but NOT saved here — UoWBehavior handles persistence.
/// </summary>
internal sealed class GeminiService(
    HttpClient httpClient,
    IOptions<GeminiAISettings> settings,
    AiDbContext dbContext,
    ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private GeminiAISettings Config => settings.Value;

    private string BuildApiUrl() => Config.BuildApiUrl();

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

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("Evaluate", prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(json, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            var score = Math.Clamp(parsed.QualityScore, 0, 5);
            return new EvaluationResult(score, parsed.IsCorrect, parsed.SuggestedCorrection ?? "", parsed.Explanation ?? "", parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse evaluation response: {Json}", json);
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
              "qualityScore": <int 0-5 where 0=empty/no speech, 1=completely wrong/unintelligible, 2=partially wrong/poor pronunciation, 3=correct but with noticeable pronunciation/grammar mistakes, 4=correct with minor issues, 5=perfect native-like>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer if they made a mistake>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong with their pronunciation or grammar>"
            }
            """;

        var base64Audio = Convert.ToBase64String(audioData);
        var requestBody = new
        {
            contents = new[] { 
                new { 
                    role = "user", 
                    parts = new object[] 
                    { 
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = base64Audio } }
                    } 
                } 
            },
            generationConfig = BuildGenerationConfig()
        };

        var (json, _, _, _) = await ExecuteGeminiRequestAsync("EvaluateAudio", requestBody, prompt, cancellationToken);

        try
        {
            var parsed = JsonSerializer.Deserialize<EvaluationJsonResponse>(json, JsonOptions);
            if (parsed is null)
                return new EvaluationResult(0, false, "", "Không thể phân tích phản hồi từ AI.");

            var score = Math.Clamp(parsed.QualityScore, 0, 5);
            return new EvaluationResult(score, parsed.IsCorrect, parsed.SuggestedCorrection ?? "", parsed.Explanation ?? "", parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse audio evaluation response: {Json}", json);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }



    public async Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        // history already contains the user's latest message added by ChatSession.AddUserMessage()
        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = history.Select(h => new
            {
                role = h.Role,
                parts = new[] { new { text = h.Text } }
            }).ToArray(),
            generationConfig = BuildGenerationConfig(requireJsonResponse: false),
            safetySettings = BuildSafetySettings()
        };
        var sw = Stopwatch.StartNew();
        string responseText;
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            var url = BuildApiUrl();
            var content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(responseJson);
            responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = "";
            logger.LogError(ex, "[Gemini] Chat failed");
        }
        finally
        {
            sw.Stop();
        }

        // Queue audit log — saved by UoWBehavior, not here
        var log = AiRequestLog.Create("Chat", userMessage, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg);
        dbContext.AiRequestLogs.Add(log);

        logger.LogInformation("[Gemini] Chat completed in {Ms}ms (success={Success})", sw.ElapsedMilliseconds, isSuccess);
        return responseText;
    }

    // ── Content Generation Methods ──────────────────────────────────────────

    private const string SystemIntro =
        "You are an AI assistant powering an English learning app. " +
        "Generate accurate, pedagogically sound, and learner-friendly educational content for English learners. " +
        "Follow the output format exactly. Output only valid JSON, no markdown.";

    private static string BuildTranslationsInstruction(List<string>? targetLanguageCodes)
    {
        if (targetLanguageCodes is null or { Count: 0 })
            return "\"translations\": {}";
        var langs = string.Join(", ", targetLanguageCodes.Select(c => $"\"{c}\": \"<translation in {c}>\""));
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

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateVocabulary", prompt, cancellationToken);
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
            logger.LogWarning(ex, "[Gemini] Failed to parse vocabulary response: {Json}", json);
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

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateGrammar", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<GrammarJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(g =>
            {
                var examples = (g.Examples ?? [])
                    .Select(e => new GeminiGrammarExample(
                        e.Sentence ?? "",
                        e.Note,
                        e.Translations ?? []))
                    .ToList();
                return new GeminiGeneratedGrammar(g.Title ?? title, g.Explanation, g.Formula, g.Level ?? level, examples);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse grammar response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedGrammar(title, null, null, level, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedConversation>> GenerateFullConversationAsync(
        string topicName, string? level, string? scenario, int lineCount,
        List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 10);
        var levelHint = level is not null ? $"Language difficulty: CEFR {level}." : "";
        var scenarioHint = scenario is not null ? $"Scenario: {scenario}." : "";
        var scope = count == 1
            ? $"Create a realistic English dialogue about: \"{topicName}\"."
            : $"Create {count} different realistic English dialogues about: \"{topicName}\".";
        var translationsInstruction = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}

            {{scope}} {{levelHint}} {{scenarioHint}}
            Each dialogue must have exactly {{lineCount}} lines.

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<descriptive title for this conversation>",
                  "lines": [
                    {
                      "speaker": "<Speaker name, e.g. 'Alex' or 'Barista'>",
                      "text": "<English dialogue line>",
                      {{translationsInstruction}}
                    }
                  ]
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateFullConversation", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<FullConversationJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(c =>
            {
                var lines = (c.Lines ?? [])
                    .Select(l => new GeminiConversationLine(
                        l.Speaker ?? "Speaker",
                        l.Text ?? "",
                        l.Translations ?? []))
                    .ToList();
                return new GeminiGeneratedConversation(c.Title ?? topicName, lines);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse full conversation response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedConversation(topicName, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedExercise>> GenerateExerciseAsync(
        string context, int exerciseType, string? level, int count = 1, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var levelHint = level is not null ? $"Difficulty: CEFR {level}." : "";

        // Type-specific options instruction so Gemini knows exact option count and format
        var optionsNote = exerciseType switch
        {
            0 => // MultipleChoice: exactly 4 options, exactly 1 correct
                "\"options\": [{ \"text\": \"<option>\", \"isCorrect\": true/false }] // provide exactly 4 options, 1 must be correct",
            2 => // TrueFalse: exactly 2 options
                "\"options\": [{ \"text\": \"True\", \"isCorrect\": true/false }, { \"text\": \"False\", \"isCorrect\": true/false }]",
            14 => // TrueFalseNotGiven: exactly 3 options, exactly 1 correct
                "\"options\": [{ \"text\": \"True\", \"isCorrect\": true/false }, { \"text\": \"False\", \"isCorrect\": false }, { \"text\": \"Not Given\", \"isCorrect\": false }] // set isCorrect=true on the single correct option",
            _ =>
                "\"options\": []"
        };

        var scope = count == 1
            ? "Generate 1 exercise"
            : $"Generate {count} different exercises (use the same context but vary the question/angle)";

        // All AI-supported types with their numeric IDs
        var typeNames =
            "0=MultipleChoice, 1=FillInTheBlank, 2=TrueFalse, 4=Translation, " +
            "9=SentenceCompletion, 10=WordForm, 11=VerbTense, 14=TrueFalseNotGiven, " +
            "18=Paraphrasing, 19=GapFilling, 22=ReorderingWords, 23=RewritingSentences, " +
            "24=DialogueCompletion, 27=ShortAnswerQuestions";

        var prompt = $$"""
            {{SystemIntro}}

            {{scope}} for English learners.
            Context / topic: {{context}}
            Exercise type: {{exerciseType}} ({{typeNames}})
            {{levelHint}}

            Respond ONLY with this exact JSON object:
            {
              "items": [
                {
                  "title": "<short descriptive title>",
                  "instruction": "<clear learner instruction, e.g. 'Choose the correct answer'>",
                  "content": "<question or passage text, or null>",
                  "correctAnswer": "<exact correct answer text>",
                  "explanation": "<brief explanation why this is correct, can be in Vietnamese>",
                  {{optionsNote}}
                }
              ]
            }
            """;

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateExercise", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExerciseJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(e =>
            {
                var options = (e.Options ?? [])
                    .Select(o => new GeminiExerciseOption(o.Text ?? "", o.IsCorrect))
                    .ToList();
                return new GeminiGeneratedExercise(
                    e.Title ?? "Exercise", e.Instruction ?? "",
                    e.Content, e.CorrectAnswer ?? "", e.Explanation, options);
            }).ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse exercise response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedExercise("Exercise", "", null, "", null, [])] : [];
        }
    }


    public async Task<List<GeminiGeneratedExampleSentence>> GenerateExampleSentencesAsync(
        string context, int count, string? level, List<string>? targetLanguageCodes = null,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        var levelHint = level is not null ? $"CEFR level: {level}." : "";
        var translationsInstruction = BuildTranslationsInstruction(targetLanguageCodes);

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} natural English example sentences illustrating:
            {{context}}
            {{levelHint}}

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

        var (json, _, _, _) = await CallGeminiWithLoggingAsync("GenerateExampleSentences", prompt, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExampleSentenceJson>>(json, JsonOptions);
            return (wrapper?.Items ?? [])
                .Select(s => new GeminiGeneratedExampleSentence(s.Text ?? "", s.Note, s.Translations ?? []))
                .ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[Gemini] Failed to parse example sentences response: {Json}", json);
            return [];
        }
    }


    private object BuildGenerationConfig(bool requireJsonResponse = true)
    {
        var config = new Dictionary<string, object>
        {
            ["maxOutputTokens"] = Config.MaxOutputTokens,
            ["temperature"] = Config.Temperature,
            ["topP"] = Config.TopP
        };

        if (requireJsonResponse)
        {
            config["responseMimeType"] = "application/json";
        }

        if (!string.IsNullOrWhiteSpace(Config.ThinkingLevel))
        {
            config["thinkingConfig"] = new { thinkingLevel = Config.ThinkingLevel };
        }

        return config;
    }

    private object[]? BuildSafetySettings()
    {
        if (!Config.DisableAllSafetyRestrictions)
            return null;

        return new[]
        {
            new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "OFF" },
            new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_HARASSMENT", threshold = "OFF" }
        };
    }

    /// <summary>
    /// Calls Gemini with structured JSON output and queues an audit log entry.
    /// Does NOT call SaveChanges — caller's UoWBehavior handles persistence.
    /// </summary>
    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> CallGeminiWithLoggingAsync(
        string requestType, string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            contents = new[] { new { role = "user", parts = new object[] { new { text = prompt } } } },
            generationConfig = BuildGenerationConfig(),
            safetySettings = BuildSafetySettings()
        };

        return await ExecuteGeminiRequestAsync(requestType, requestBody, prompt, cancellationToken);
    }

    private async Task<(string Json, long LatencyMs, bool IsSuccess, string? Error)> ExecuteGeminiRequestAsync(
        string requestType, object requestBody, string promptLog, CancellationToken cancellationToken)
    {
        var url = BuildApiUrl();

        var content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        string responseText;
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[Gemini] Calling {RequestType} on model {Model}", requestType, Config.ModelName);

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(responseJson);
            responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = "{}";
            logger.LogError(ex, "[Gemini] {RequestType} failed", requestType);
        }
        finally
        {
            sw.Stop();
        }

        // Queue audit log — saved by UoWBehavior at end of command pipeline
        var log = AiRequestLog.Create(requestType, promptLog, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg);
        dbContext.AiRequestLogs.Add(log);

        logger.LogInformation("[Gemini] {RequestType} completed in {Ms}ms (success={Success})", requestType, sw.ElapsedMilliseconds, isSuccess);
        return (responseText, sw.ElapsedMilliseconds, isSuccess, errorMsg);
    }

    // ── Internal JSON DTOs ──

    private sealed class EvaluationJsonResponse
    {
        public int QualityScore { get; set; }
        public bool IsCorrect { get; set; }
        public string? SuggestedCorrection { get; set; }
        public string? Explanation { get; set; }
        public string? Transcription { get; set; }
    }


    // ── Content Generation Internal DTOs ──

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

