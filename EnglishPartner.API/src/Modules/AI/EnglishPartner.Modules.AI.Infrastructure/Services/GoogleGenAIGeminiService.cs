using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Settings;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Alternative implementation of IGeminiService using the unified Google.GenAI SDK.
/// This connects to the Gemini API (Google AI Studio) using the ApiKey.
/// </summary>
internal sealed class GoogleGenAIGeminiService(
    IOptions<GeminiAISettings> settings,
    AiDbContext dbContext,
    ILogger<GoogleGenAIGeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private GeminiAISettings Config => settings.Value;

    // ── Client ───────────────────────────────────────────────────────────────

    private Client CreateClient()
    {
        var httpOptions = new HttpOptions
        {
            Timeout = (int)TimeSpan.FromSeconds(Config.TimeoutSeconds).TotalMilliseconds
        };

        // Prioritize Google AI Studio (API Key)
        if (!string.IsNullOrWhiteSpace(Config.ApiKey))
        {
            return new Client(apiKey: Config.ApiKey, httpOptions: httpOptions);
        }

        // Fallback to Vertex AI endpoint (Google Cloud ADC)
        if (!string.IsNullOrWhiteSpace(Config.ProjectId))
        {
            return new Client(vertexAI: true, project: Config.ProjectId, location: Config.Location, httpOptions: httpOptions);
        }

        throw new InvalidOperationException("Either ApiKey (for Gemini API) or ProjectId (for Vertex AI) must be configured in GeminiAISettings.");
    }

    private GenerateContentConfig BuildGenerationConfig(bool requireJsonResponse = true, string? modelName = null)
    {
        var cfg = new GenerateContentConfig
        {
            MaxOutputTokens = Config.MaxOutputTokens,
            Temperature = (float)Config.Temperature,
            TopP = (float)Config.TopP
        };

        if (requireJsonResponse && SupportsJsonMode(modelName ?? Config.ModelName))
            cfg.ResponseMimeType = "application/json";

        if (Config.DisableAllSafetyRestrictions)
        {
            cfg.SafetySettings =
            [
                new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.BlockNone },
                new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.BlockNone },
                new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.BlockNone },
                new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.BlockNone }
            ];
        }

        return cfg;
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
                Math.Clamp(parsed.Score, 0, 100),
                parsed.IsCorrect,
                parsed.SuggestedCorrection ?? "",
                parsed.Explanation ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[GoogleGenAI] Failed to parse evaluation response: {Json}", json);
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
              "score": <int 0-100>,
              "isCorrect": <bool>,
              "suggestedCorrection": "<the correct version of the answer if they made a mistake>",
              "explanation": "<brief explanation in Vietnamese of what was right/wrong>"
            }
            """;

        var client = CreateClient();
        var config = BuildGenerationConfig(requireJsonResponse: true, modelName: Config.ModelName);

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts =
                [
                    new Part { Text = prompt },
                    new Part
                    {
                        InlineData = new Blob
                        {
                            MimeType = mimeType,
                            Data = audioData // Google.GenAI usually takes byte[] for Data
                        }
                    }
                ]
            }
        };

        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            logger.LogInformation("[GoogleGenAI] Calling EvaluateAudio on model {Model}", Config.ModelName);
            var response = await client.Models.GenerateContentAsync(Config.ModelName, contents, config, cancellationToken);
            responseText = response.Text ?? "{}";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[GoogleGenAI] EvaluateAudio failed");
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
                Math.Clamp(parsed.Score, 0, 100),
                parsed.IsCorrect,
                parsed.SuggestedCorrection ?? "",
                parsed.Explanation ?? "",
                parsed.Transcription ?? "");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[GoogleGenAI] Failed to parse audio evaluation response: {Json}", responseText);
            return new EvaluationResult(0, false, "", "AI trả lời không hợp lệ. Vui lòng thử lại.");
        }
    }

    // ── Chat ─────────────────────────────────────────────────────────────────

    public async Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var config = BuildGenerationConfig(requireJsonResponse: false, modelName: Config.ModelName);
        config.SystemInstruction = new Content
        {
            Role = "system",
            Parts = [new Part { Text = systemPrompt }]
        };

        var contents = new List<Content>();
        foreach (var h in history)
        {
            contents.Add(new Content
            {
                Role = h.Role,
                Parts = [new Part { Text = h.Text }]
            });
        }

        var sw = Stopwatch.StartNew();
        string responseText = "";
        bool isSuccess = true;
        string? errorMsg = null;

        try
        {
            var response = await client.Models.GenerateContentAsync(
                model: Config.ModelName,
                contents: contents,
                config: config,
                cancellationToken: cancellationToken);
            responseText = response.Text ?? "";
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            logger.LogError(ex, "[GoogleGenAI] Chat failed");
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create("Chat", userMessage, responseText, Config.ModelName, sw.ElapsedMilliseconds, isSuccess, errorMsg));

        logger.LogInformation("[GoogleGenAI] Chat completed in {Ms}ms (success={Success})", sw.ElapsedMilliseconds, isSuccess);
        return responseText;
    }

    // ── History Summarization ────────────────────────────────────────────────

    public async Task<string> SummarizeHistoryAsync(string? previousSummary, IReadOnlyList<ChatTurn> messages, CancellationToken cancellationToken = default)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are summarizing a language tutoring conversation for context continuity.");
        sb.AppendLine("Create a concise summary (under 300 words) capturing:");
        sb.AppendLine("- Key topics and vocabulary discussed");
        sb.AppendLine("- Student's mistakes and corrections made");
        sb.AppendLine("- Grammar points covered");
        sb.AppendLine("- Student's proficiency observations");
        sb.AppendLine("- Any ongoing exercises or tasks");
        sb.AppendLine("Output only the summary text, no formatting or markdown.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(previousSummary))
        {
            sb.AppendLine("Previous conversation summary:");
            sb.AppendLine(previousSummary);
            sb.AppendLine();
        }

        sb.AppendLine("New messages to incorporate:");
        foreach (var m in messages)
        {
            var label = m.Role == "user" ? "Student" : "Tutor";
            sb.AppendLine($"{label}: {m.Text}");
        }

        var prompt = sb.ToString();
        var (text, _, _, _) = await CallWithLoggingAsync("SummarizeHistory", prompt, null, false, cancellationToken);
        return text.Trim();
    }

    // ── Content Generation ───────────────────────────────────────────────────


    public async Task<string> TranslateAsync(string text, string targetLanguageCode, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            You are a highly accurate, professional translator.
            Translate the following text into the language represented by the code '{{targetLanguageCode}}'.
            Do not add any explanations, notes, or quotes. Output ONLY the translated text.
            
            Original text:
            {{text}}
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("Translate", prompt, null, false, cancellationToken);
        return json.Trim();
    }

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

    private bool SupportsJsonMode(string model)
    {
        return model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase);
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
            logger.LogWarning(ex, "[GoogleGenAI] Failed to parse vocabulary response: {Json}", json);
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
            logger.LogWarning(ex, "[GoogleGenAI] Failed to parse grammar response: {Json}", json);
            return count == 1 ? [new GeminiGeneratedGrammar(title, null, null, level, [])] : [];
        }
    }

    public async Task<List<GeminiGeneratedConversation>> GenerateFullConversationAsync(
        string targetLanguage,
        string topicName,
        string? level,
        string? scenario,
        int lineCount,
        List<string>? targetLanguageCodes = null,
        int count = 1,
        CancellationToken cancellationToken = default)
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
            logger.LogWarning(ex, "[GoogleGenAI] Failed parse conversation: {Json}", json);
            return [];
        }
    }

    public async Task<List<GeminiGeneratedExercise>> GenerateExerciseAsync(
        string targetLanguage,
        string context,
        int exerciseType,
        string? level,
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        var typeName = GetExerciseTypeName(exerciseType);
        var optionCount = GetDefaultOptionCount(exerciseType);
        var levelHint = level is not null ? $"Target CEFR level: {level}." : "Determine the appropriate level.";

        var optionsInstruction = optionCount > 0
            ? $"""
            Each exercise MUST include exactly {optionCount} answer options in the "options" array.
            Exactly one option must have "isCorrect": true and its "text" must match "correctAnswer".
            The remaining {optionCount - 1} option(s) must be plausible distractors with "isCorrect": false.
            {GetTypeSpecificHint(exerciseType)}
            """
            : """
            This exercise type does NOT use selectable options. Return "options": [] (empty array).
            """;

        var prompt = $$"""
            {{SystemIntro}}

            Generate {{count}} exercise(s) of type "{{typeName}}" for learning {{targetLanguage}}.
            Context: "{{context}}".
            {{levelHint}}

            {{optionsInstruction}}

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

    public async Task<List<GeminiExerciseOption>> GenerateExerciseOptionsAsync(
        string targetLanguage,
        int exerciseType,
        string title,
        string instruction,
        string? content,
        string correctAnswer,
        int optionCount = 4,
        CancellationToken cancellationToken = default)
    {
        var typeName = GetExerciseTypeName(exerciseType);
        var resolvedCount = optionCount > 0 ? optionCount : GetDefaultOptionCount(exerciseType);
        resolvedCount = Math.Clamp(resolvedCount, 2, 6);
        var typeHint = GetTypeSpecificHint(exerciseType);

        var prompt = $$"""
            {{SystemIntro}}

            I have a "{{typeName}}" exercise for learning {{targetLanguage}}:
            - Title: "{{title}}"
            - Instruction: "{{instruction}}"
            - Content: "{{content ?? ""}}"
            - Correct Answer: "{{correctAnswer}}"

            Generate exactly {{resolvedCount}} answer options for this exercise.
            Exactly one option MUST have "isCorrect": true and its "text" must match the correct answer.
            The remaining option(s) must be plausible distractors with "isCorrect": false.
            {{typeHint}}

            Respond ONLY with this exact JSON object:
            {
              "items": [
                { "text": "<option text>", "isCorrect": <bool> }
              ]
            }
            """;

        var (json, _, _, _) = await CallWithLoggingAsync("GenerateExerciseOptions", prompt, null, true, cancellationToken);
        try
        {
            var wrapper = JsonSerializer.Deserialize<ItemsWrapper<ExerciseOptionJson>>(json, JsonOptions);
            return (wrapper?.Items ?? []).Select(o => new GeminiExerciseOption(o.Text ?? "", o.IsCorrect)).ToList();
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
        string requestType,
        string prompt,
        string? systemInstruction,
        bool requireJson,
        CancellationToken ct)
    {
        var client = CreateClient();
        var config = BuildGenerationConfig(requireJson, modelName: Config.ModelName);

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            config.SystemInstruction = new Content { Role = "system", Parts = [new Part { Text = systemInstruction }] };
        }

        var contents = new List<Content>
        {
            new Content { Role = "user", Parts = [new Part { Text = prompt }] }
        };

        var sw = Stopwatch.StartNew();
        string responseText = "{}";
        bool isSuccess = true;
        string? errorMsg = null;
        int? inputTokens = null;
        int? outputTokens = null;

        try
        {
            logger.LogInformation("[GoogleGenAI] Calling {RequestType} on model {Model}", requestType, Config.ModelName);
            var response = await client.Models.GenerateContentAsync(
                model: Config.ModelName,
                contents: contents,
                config: config,
                cancellationToken: ct);
            responseText = response.Text ?? (requireJson ? "{}" : "");
            
            if (response.UsageMetadata != null)
            {
                inputTokens = response.UsageMetadata.PromptTokenCount;
                outputTokens = response.UsageMetadata.CandidatesTokenCount;
            }

            if (requireJson)
            {
                responseText = CleanJsonMarkdown(responseText);
            }
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMsg = ex.Message;
            responseText = requireJson ? "{}" : "";
            logger.LogError(ex, "[GoogleGenAI] {RequestType} failed", requestType);
        }
        finally { sw.Stop(); }

        dbContext.AiRequestLogs.Add(
            AiRequestLog.Create(
                requestType: requestType,
                inputText: prompt,
                outputText: responseText,
                modelName: Config.ModelName,
                latencyMs: sw.ElapsedMilliseconds,
                isSuccess: isSuccess,
                errorMessage: errorMsg,
                inputTokens: inputTokens,
                outputTokens: outputTokens));

        await dbContext.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("[GoogleGenAI] {RequestType} completed in {Ms}ms (success={Success})", requestType, sw.ElapsedMilliseconds, isSuccess);

        return (responseText, sw.ElapsedMilliseconds, isSuccess, errorMsg);
    }

    private static string CleanJsonMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";
        
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring("```json".Length).TrimStart();
        }
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring("```".Length).TrimStart();
        }

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - "```".Length).TrimEnd();
        }

        return trimmed;
    }

    // ── Exercise type helpers ────────────────────────────────────────────────
    // Maps mirror ExerciseType enum values from Curriculum.Domain.
    // AI module cannot reference Curriculum.Domain directly (cross-module rule).

    private static string GetExerciseTypeName(int type) => type switch
    {
        0  => "MultipleChoice",
        1  => "FillInTheBlank",
        2  => "TrueFalse",
        3  => "Matching",
        4  => "Translation",
        5  => "SentenceCompletion",
        6  => "WordForm",
        7  => "VerbTense",
        8  => "TrueFalseNotGiven",
        9  => "Paraphrasing",
        10 => "ReorderingWords",
        11 => "RewritingSentences",
        12 => "DialogueCompletion",
        13 => "ShortAnswerQuestions",
        14 => "QuestionFormation",
        15 => "SynonymAntonym",
        16 => "Collocation",
        17 => "OddOneOut",
        18 => "SpellingCorrection",
        19 => "WordStress",
        20 => "MultipleResponse",
        21 => "SentenceCorrection",
        22 => "Categorization",
        23 => "ListeningComprehension",
        24 => "ListeningGapFill",
        25 => "Speaking",
        26 => "ReadingComprehension",
        27 => "Writing",
        28 => "Dictation",
        29 => "PictureDescription",
        30 => "TopicResponse",
        _  => $"Type{type}"
    };

    private static int GetDefaultOptionCount(int type) => type switch
    {
        0  => 4, // MultipleChoice
        1  => 4, // FillInTheBlank
        2  => 2, // TrueFalse
        5  => 4, // SentenceCompletion
        6  => 4, // WordForm
        7  => 4, // VerbTense
        8  => 3, // TrueFalseNotGiven
        12 => 4, // DialogueCompletion
        14 => 4, // QuestionFormation
        15 => 4, // SynonymAntonym
        16 => 4, // Collocation
        17 => 4, // OddOneOut
        19 => 4, // WordStress
        20 => 5, // MultipleResponse (≥2 correct)
        _  => 0
    };

    private static string GetTypeSpecificHint(int type) => type switch
    {
        2  => "The only 2 options must be exactly \"True\" and \"False\".",
        8  => "The only 3 options must be exactly \"True\", \"False\", and \"Not Given\".",
        15 => "Options should be words related to the target word. One is the correct synonym/antonym.",
        16 => "Options should be words that could collocate with the given word. Only one is natural.",
        17 => "All 4 options are words. 3 belong to the same category, 1 does NOT. Mark the odd one as correct.",
        19 => "Options represent syllable stress positions (e.g. \"1st syllable\", \"2nd syllable\"). One is correct.",
        20 => "This is a multiple-response question. At least 2 options must have \"isCorrect\": true.",
        _  => ""
    };

    // ── Internal JSON DTOs ───────────────────────────────────────────────────


    private sealed class EvaluationJsonResponse
    {
        public int Score { get; set; }
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

