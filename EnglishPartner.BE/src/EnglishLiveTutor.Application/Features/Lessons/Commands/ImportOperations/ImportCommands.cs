using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;

public sealed record BuildLessonPromptCommand(
    string Topic,
    string CefrLevel,
    string? LessonTitle = null,
    List<string>? PreferredExerciseTypes = null,
    int TargetExerciseCount = 5,
    string? CustomInstructions = null
) : ICommand<LessonPromptResultDto>;

public sealed record GenerateLessonExercisesCommand(
    string? ApiKey,
    string? ModelCode,
    Guid? TargetLessonId,
    string Topic,
    string CefrLevel,
    string? LessonTitle = null,
    List<string>? PreferredExerciseTypes = null,
    int TargetExerciseCount = 5,
    string? CustomInstructions = null,
    string CommitMode = "PreviewOnly"
) : ICommand<LessonCandidateValidationResultDto>;

public sealed record PreviewLessonImportCommand(
    string RawJson
) : ICommand<LessonCandidateValidationResultDto>;

public sealed record CommitLessonImportCommand(
    CommitLessonImportRequest Request,
    string? OperationKey = null
) : ICommand<LessonDetailAuthorDto>;

public sealed class ImportCommandHandlers :
    ICommandHandler<BuildLessonPromptCommand, LessonPromptResultDto>,
    ICommandHandler<PreviewLessonImportCommand, LessonCandidateValidationResultDto>,
    ICommandHandler<CommitLessonImportCommand, LessonDetailAuthorDto>,
    ICommandHandler<GenerateLessonExercisesCommand, LessonCandidateValidationResultDto>
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

    private readonly IAppDbContext _db;
    private readonly IQueryHandler<GetLessonByIdQuery, object?> _getLessonByIdHandler;

    public ImportCommandHandlers(
        IAppDbContext db,
        IQueryHandler<GetLessonByIdQuery, object?> getLessonByIdHandler)
    {
        _db = db;
        _getLessonByIdHandler = getLessonByIdHandler;
    }

    public Task<LessonPromptResultDto> HandleAsync(BuildLessonPromptCommand command, CancellationToken ct = default)
    {
        string title = !string.IsNullOrWhiteSpace(command.LessonTitle) ? command.LessonTitle : $"English Lesson: {command.Topic}";
        var preferredTypes = (command.PreferredExerciseTypes != null && command.PreferredExerciseTypes.Count > 0)
            ? command.PreferredExerciseTypes
            : new List<string> { "Flashcards", "MultipleChoice", "FillInTheBlanks", "SentenceUnscramble", "Translation" };

        string exerciseTypeSpecifications = BuildExerciseTypeSpecifications(preferredTypes);
        string exerciseExamplesJson = BuildConcreteExerciseExamples(preferredTypes, command.Topic, command.CefrLevel);
        string exerciseRules = BuildExerciseTypeRules(preferredTypes, command.TargetExerciseCount);

        string systemPrompt = @"You are an expert ESL Curriculum Designer. Your task is to generate complete, high-quality English lessons matching schema 'lesson-exercises.v1'.
Always return valid JSON adhering strictly to the Lesson Exercises JSON Schema with exact PascalCase ExerciseType names and compliant content/grading payloads.";

        string userPrompt = $@"Generate an English lesson with interactive exercises:
- Title: {title}
- Topic: {command.Topic}
- CEFR Level: {command.CefrLevel}
- Target Exercise Count: {command.TargetExerciseCount}
- Preferred Exercise Types: {string.Join(", ", preferredTypes)}
{(string.IsNullOrWhiteSpace(command.CustomInstructions) ? "" : $"- Teacher Custom Instructions: {command.CustomInstructions}")}

CRITICAL RULES FOR EXERCISES:
{exerciseRules}

DETAILED SPECIFICATIONS FOR REQUESTED TYPES:
{exerciseTypeSpecifications}

ENVELOPE JSON STRUCTURE & EXAMPLES:
{{
  ""$schema"": ""https://raw.githubusercontent.com/.../lesson-exercises-v1.schema.json"",
  ""schemaVersion"": ""lesson-exercises.v1"",
  ""lesson"": {{
    ""title"": ""{title}"",
    ""cefrLevel"": ""{command.CefrLevel}"",
    ""topic"": ""{command.Topic}"",
    ""learningObjectives"": [
      ""Understand core vocabulary and phrases for {command.Topic}"",
      ""Construct natural English sentences appropriate for {command.CefrLevel} level""
    ],
    ""targetVocabulary"": [""daily routine"", ""wake up"", ""commute"", ""work remotely"", ""attend meetings"", ""schedule"", ""work-life balance""],
    ""targetGrammar"": [""Present Simple"", ""Adverbs of Frequency"", ""Time Expressions""],
    ""content"": ""This lesson focuses on practical language for {command.Topic} at CEFR {command.CefrLevel} level."",
    ""estimatedMinutes"": 30
  }},
  ""exercises"": [
{exerciseExamplesJson}
  ]
}}";

        string promptText = $@"{systemPrompt}

{userPrompt}";

        return Task.FromResult(new LessonPromptResultDto(
            SystemPrompt: systemPrompt,
            UserPrompt: userPrompt,
            PromptText: promptText,
            SchemaVersion: "lesson-exercises.v1"
        ));
    }

    public async Task<LessonCandidateValidationResultDto> HandleAsync(GenerateLessonExercisesCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ApiKey))
        {
            throw new InvalidOperationException("Vui lòng cung cấp Google Gemini API Key để sinh bài tập AI.");
        }

        string topic = command.Topic;
        string cefr = command.CefrLevel;
        string title = !string.IsNullOrWhiteSpace(command.LessonTitle) ? command.LessonTitle : $"Lesson: {topic}";

        if (command.TargetLessonId.HasValue && command.TargetLessonId.Value != Guid.Empty)
        {
            var targetLesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == command.TargetLessonId.Value, ct);
            if (targetLesson != null)
            {
                topic = targetLesson.Topic;
                cefr = targetLesson.CefrLevel.ToString();
                title = targetLesson.Title;
            }
        }

        string requestedModel = string.IsNullOrWhiteSpace(command.ModelCode) ? "gemini-3.8-flash" : command.ModelCode.Trim();
        var modelEntity = await _db.AiModelCatalog.AsNoTracking().FirstOrDefaultAsync(m => m.ModelCode == requestedModel, ct);
        string effectiveModel = modelEntity != null ? modelEntity.GetEffectiveModelCode() : requestedModel;

        var chosenTypes = (command.PreferredExerciseTypes != null && command.PreferredExerciseTypes.Count > 0)
            ? command.PreferredExerciseTypes
            : new List<string> { "Flashcards", "MultipleChoice", "FillInTheBlanks", "SentenceUnscramble", "Translation" };

        string exerciseSpecs = BuildExerciseTypeSpecifications(chosenTypes);
        string exerciseExamples = BuildConcreteExerciseExamples(chosenTypes, topic, cefr);
        string exerciseRules = BuildExerciseTypeRules(chosenTypes, command.TargetExerciseCount);

        string prompt = $@"You are an expert ESL Curriculum Designer. Your task is to generate complete, high-quality English exercises matching schema 'lesson-exercises.v1'.
Always return a SINGLE, valid JSON object adhering strictly to the Lesson Exercises JSON Schema with exact PascalCase ExerciseType names and compliant content/grading payloads.
NO markdown code block fences (no ```json), NO introductory or concluding text.

Lesson Context:
- Title: {title}
- CEFR Level: {cefr}
- Topic: {topic}
- Target Exercise Count: {command.TargetExerciseCount}
- Allowed/Preferred Exercise Types: {string.Join(", ", chosenTypes)}
{(string.IsNullOrWhiteSpace(command.CustomInstructions) ? "" : $"- Additional Teacher Instructions: {command.CustomInstructions}")}

CRITICAL RULES FOR EXERCISES:
{exerciseRules}

EXERCISE SCHEMA SPECIFICATIONS FOR ALLOWED TYPES:
{exerciseSpecs}

OUTPUT JSON STRUCTURE:
{{
  ""schemaVersion"": ""lesson-exercises.v1"",
  ""lesson"": {{
    ""title"": ""{title}"",
    ""cefrLevel"": ""{cefr}"",
    ""topic"": ""{topic}"",
    ""estimatedMinutes"": 30,
    ""learningObjectives"": [""Understand core vocabulary for {topic}"", ""Practice communicative sentences""],
    ""targetVocabulary"": [""daily routine"", ""wake up"", ""commute"", ""work remotely"", ""attend meetings"", ""schedule"", ""work-life balance""],
    ""targetGrammar"": [""Present Simple"", ""Adverbs of Frequency"", ""Time Expressions""],
    ""content"": ""Overview context for {topic}...""
  }},
  ""exercises"": [
{exerciseExamples}
  ]
}}";

        var httpClient = _httpClient;

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{effectiveModel}:generateContent?key={Uri.EscapeDataString(command.ApiKey.Trim())}";
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.3,
                responseMimeType = "application/json"
            }
        };

        using var resp = await httpClient.PostAsJsonAsync(url, payload, ct);
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google AI Studio trả về mã lỗi {(int)resp.StatusCode}: {rawBody}");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về nội dung JSON bài tập hợp lệ.");
        }

        // Validate generated JSON candidate
        var validationResult = await HandleAsync(new PreviewLessonImportCommand(jsonText), ct);

        // If commit mode is Append or Replace and candidate is valid and target lesson ID is provided, commit it!
        if (command.CommitMode is "Append" or "Replace" && command.TargetLessonId.HasValue && validationResult.IsValid && validationResult.Candidate != null)
        {
            var commitReq = new CommitLessonImportRequest(
                TargetLessonId: command.TargetLessonId.Value,
                TargetRevision: null,
                CommitMode: command.CommitMode,
                Candidate: validationResult.Candidate
            );
            await HandleAsync(new CommitLessonImportCommand(commitReq), ct);
        }

        return validationResult;
    }

    public Task<LessonCandidateValidationResultDto> HandleAsync(PreviewLessonImportCommand command, CancellationToken ct = default)
    {
        var issues = new List<ValidationIssueDto>();
        JsonNode? rootNode = null;

        try
        {
            rootNode = JsonNode.Parse(command.RawJson);
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssueDto("$", "Error", $"Malformed JSON syntax: {ex.Message}"));
            return Task.FromResult(new LessonCandidateValidationResultDto(false, issues, null));
        }

        if (rootNode is not JsonObject rootObj)
        {
            issues.Add(new ValidationIssueDto("$", "Error", "Root element must be a JSON object."));
            return Task.FromResult(new LessonCandidateValidationResultDto(false, issues, null));
        }

        var lessonNode = rootObj["lesson"];
        if (lessonNode == null)
        {
            issues.Add(new ValidationIssueDto("$.lesson", "Error", "Field 'lesson' metadata is required."));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(lessonNode["title"]?.ToString()))
                issues.Add(new ValidationIssueDto("$.lesson.title", "Error", "Lesson title is required."));
            if (string.IsNullOrWhiteSpace(lessonNode["cefrLevel"]?.ToString()))
                issues.Add(new ValidationIssueDto("$.lesson.cefrLevel", "Error", "CEFR level is required."));
            if (string.IsNullOrWhiteSpace(lessonNode["topic"]?.ToString()))
                issues.Add(new ValidationIssueDto("$.lesson.topic", "Error", "Lesson topic is required."));
        }

        var exercisesNode = rootObj["exercises"];
        if (exercisesNode is not JsonArray exercisesArray || exercisesArray.Count == 0)
        {
            issues.Add(new ValidationIssueDto("$.exercises", "Error", "At least one exercise is required."));
        }
        else
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < exercisesArray.Count; i++)
            {
                var ex = exercisesArray[i];
                string path = $"$.exercises[{i}]";
                string key = ex?["key"]?.ToString() ?? string.Empty;
                string typeStr = ex?["type"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                {
                    issues.Add(new ValidationIssueDto($"{path}.key", "Error", "Exercise key is required."));
                }
                else if (!keys.Add(key))
                {
                    issues.Add(new ValidationIssueDto($"{path}.key", "Error", $"Duplicate exercise key '{key}'."));
                }

                if (!Enum.TryParse<ExerciseType>(typeStr, ignoreCase: true, out _))
                {
                    issues.Add(new ValidationIssueDto($"{path}.type", "Error", $"Invalid or unrecognized ExerciseType '{typeStr}'."));
                }

                if (ex?["content"] == null)
                {
                    issues.Add(new ValidationIssueDto($"{path}.content", "Error", "Exercise content is required."));
                }
            }
        }

        bool isValid = issues.All(i => i.Severity != "Error");
        return Task.FromResult(new LessonCandidateValidationResultDto(isValid, issues, rootNode));
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(CommitLessonImportCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var candidateObj = req.Candidate.AsObject();

        var lessonNode = candidateObj["lesson"]
            ?? throw new ArgumentException("Candidate payload missing 'lesson' object.");
        var exercisesArray = candidateObj["exercises"]?.AsArray()
            ?? throw new ArgumentException("Candidate payload missing 'exercises' array.");

        LessonEntity lesson;
        if (req.TargetLessonId.HasValue && req.TargetLessonId.Value != Guid.Empty)
        {
            lesson = await _db.Lessons
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == req.TargetLessonId.Value, ct)
                ?? throw new NotFoundException($"Target lesson with ID '{req.TargetLessonId.Value}' not found.");

            if (req.TargetRevision.HasValue && lesson.Version != req.TargetRevision.Value)
            {
                throw new ConflictException($"CONCURRENCY_CONFLICT: Target lesson version has changed (current: {lesson.Version}, targetRevision: {req.TargetRevision.Value}).");
            }

            if (string.Equals(req.CommitMode, "Replace", StringComparison.OrdinalIgnoreCase))
            {
                var existingExIds = lesson.Exercises.Select(e => e.Id).ToList();
                foreach (var id in existingExIds)
                {
                    lesson.RemoveExercise(id);
                }
            }
        }
        else
        {
            string title = lessonNode["title"]?.ToString() ?? "Imported Lesson";
            string cefr = lessonNode["cefrLevel"]?.ToString() ?? "B1";
            string topic = lessonNode["topic"]?.ToString() ?? "General";
            string? content = lessonNode["content"]?.ToString();
            int estimatedMinutes = lessonNode["estimatedMinutes"]?.GetValue<int>() ?? 30;

            string objectivesJson = lessonNode["learningObjectives"]?.ToJsonString() ?? "[]";
            string vocabJson = lessonNode["targetVocabulary"]?.ToJsonString() ?? "[]";
            string grammarJson = lessonNode["targetGrammar"]?.ToJsonString() ?? "[]";

            lesson = LessonEntity.Create(
                title: title,
                cefrLevel: cefr,
                topic: topic,
                content: content,
                estimatedMinutes: estimatedMinutes,
                learningObjectivesJson: objectivesJson,
                targetVocabularyJson: vocabJson,
                targetGrammarJson: grammarJson);

            _db.Lessons.Add(lesson);
        }

        foreach (var ex in exercisesArray)
        {
            string key = ex?["key"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
            string typeStr = ex?["type"]?.ToString() ?? "Flashcards";
            string title = ex?["title"]?.ToString() ?? key;
            string instructions = ex?["instructions"]?.ToString() ?? string.Empty;
            decimal maxPoints = ex?["maxPoints"]?.GetValue<decimal>() ?? 1.0m;
            string gradingModeStr = ex?["gradingMode"]?.ToString() ?? "Deterministic";

            Enum.TryParse<ExerciseType>(typeStr, ignoreCase: true, out var exerciseType);
            Enum.TryParse<GradingMode>(gradingModeStr, ignoreCase: true, out var gradingMode);

            string contentJson = ex?["content"]?.ToJsonString() ?? "{}";
            string? gradingJson = ex?["grading"]?.ToJsonString();
            string? skillTagsJson = ex?["skillTags"]?.ToJsonString();

            lesson.AddExercise(
                key: key,
                type: exerciseType,
                title: title,
                instructions: instructions,
                maxPoints: maxPoints,
                gradingMode: gradingMode,
                contentJson: contentJson,
                gradingDefinitionJson: gradingJson,
                skillTagsJson: skillTagsJson);
        }

        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    private static string BuildExerciseTypeRules(IReadOnlyList<string> types, int targetCount)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        string allowedTypesStr = uniqueTypes.Count > 0 ? string.Join(", ", uniqueTypes) : "the requested types";

        sb.AppendLine($"1. Every exercise MUST have exact PascalCase 'type' chosen strictly from [{allowedTypesStr}], sequential 'order' starting from 1 to {targetCount}, unique alphanumeric 'key' (e.g. 'ex_01_item'), clear 'title', 'instructions', 'maxPoints' (0 for Flashcards, 10 for scored exercises), 'gradingMode', 'content' object, and 'grading' object.");

        int ruleNum = 2;
        foreach (var type in uniqueTypes)
        {
            switch (type.ToLowerInvariant())
            {
                case "sentenceunscramble":
                    sb.AppendLine($"{ruleNum}. For SentenceUnscramble: 'content.tokens' MUST be an array of objects with 'id' (e.g. 't-1') and 'text'. 'grading.validSequences' MUST be an array of string arrays, where each inner array lists token IDs in the grammatically correct word order.");
                    ruleNum++;
                    break;
                case "multiplechoice":
                    sb.AppendLine($"{ruleNum}. For MultipleChoice: 'content.options' MUST have 'id' and 'text'. 'grading.correctOptionIds' MUST list the correct option ID(s).");
                    ruleNum++;
                    break;
                case "fillintheblanks":
                    sb.AppendLine($"{ruleNum}. For FillInTheBlanks: 'content.textWithBlanks' MUST contain markers like [blank-1], [blank-2]. 'content.blanks' defines each blank with 'id' and 'hint'. 'grading.blankAnswers' defines 'acceptedAnswers' for each blank id.");
                    ruleNum++;
                    break;
                case "flashcards":
                    sb.AppendLine($"{ruleNum}. For Flashcards: 'content.cards' MUST be an array of items with 'id', 'term', 'ipa', 'meaning', and 'example'. 'maxPoints' MUST be 0 and 'gradingMode' MUST be 'SelfAssessment'.");
                    ruleNum++;
                    break;
                case "matching":
                    sb.AppendLine($"{ruleNum}. For Matching: 'content.leftItems' and 'content.rightItems' with 'id' and 'text'. 'grading.correctPairs' with 'leftId' and 'rightId'.");
                    ruleNum++;
                    break;
                case "translation":
                    sb.AppendLine($"{ruleNum}. For Translation: 'content.direction' ('ViToEn' or 'EnToVi'), 'content.sourceText', 'grading.rubricCode' ('LESSON_TRANSLATION'), 'grading.referenceTranslations'.");
                    ruleNum++;
                    break;
                case "truefalse":
                    sb.AppendLine($"{ruleNum}. For TrueFalse: 'content.context', 'content.statements' with 'id' and 'text'. 'grading.statementAnswers' mapping statement ID to true/false boolean.");
                    ruleNum++;
                    break;
                case "errorcorrection":
                    sb.AppendLine($"{ruleNum}. For ErrorCorrection: 'content.incorrectSentence', 'content.instructions', 'grading.referenceCorrections', 'grading.targetErrors'.");
                    ruleNum++;
                    break;
                case "spellingpractice":
                    sb.AppendLine($"{ruleNum}. For SpellingPractice: 'content.hint', 'content.letterCount', 'grading.targetWord', and optional 'grading.acceptedVariants'.");
                    ruleNum++;
                    break;
                case "speakingpractice":
                    sb.AppendLine($"{ruleNum}. For SpeakingPractice: 'content.topic', 'content.instructions', 'content.targetDurationSeconds', 'content.suggestedPrompts', 'grading.rubricCode' ('LESSON_SPEAKING').");
                    ruleNum++;
                    break;
                case "readingcomprehension":
                    sb.AppendLine($"{ruleNum}. For ReadingComprehension: 'content.passage' and 'content.subQuestions' array with 'id', 'questionKind', 'order', 'questionText', 'options', and 'grading'.");
                    ruleNum++;
                    break;
                case "listeningcomprehension":
                    sb.AppendLine($"{ruleNum}. For ListeningComprehension: 'content.transcript' and 'content.subQuestions' array with 'id', 'questionKind', 'order', 'questionText', 'options', and 'grading'.");
                    ruleNum++;
                    break;
                case "dictation":
                    sb.AppendLine($"{ruleNum}. For Dictation: 'content.hint' and 'grading.referenceTranscript', 'grading.caseSensitive', 'grading.ignorePunctuation'.");
                    ruleNum++;
                    break;
                case "shadowing":
                    sb.AppendLine($"{ruleNum}. For Shadowing: 'content.transcript' to repeat aloud, 'content.speechRateWpm' (e.g. 110-140), 'grading.rubricCode' ('LESSON_SHADOWING'), 'grading.rubricVersion' (1).");
                    ruleNum++;
                    break;
                case "conversationroleplay":
                    sb.AppendLine($"{ruleNum}. For ConversationRolePlay: 'content.scenario', 'content.aiRole', 'content.learnerRole', 'content.initialGreeting', 'content.minTurns', 'content.maxTurns', 'grading.rubricCode' ('LESSON_ROLEPLAY').");
                    ruleNum++;
                    break;
                case "writingpractice":
                    sb.AppendLine($"{ruleNum}. For WritingPractice: 'content.prompt', 'content.minWords', 'content.maxWords', 'grading.rubricCode' ('LESSON_WRITING'), 'grading.guidelines'.");
                    ruleNum++;
                    break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildExerciseTypeSpecifications(IReadOnlyList<string> types)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var type in uniqueTypes)
        {
            switch (type.ToLowerInvariant())
            {
                case "sentenceunscramble":
                    sb.AppendLine(@"### SentenceUnscramble:
- type: ""SentenceUnscramble""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""prompt"": ""Rearrange tokens into a grammatically correct sentence:"",
    ""tokens"": [
      { ""id"": ""t-1"", ""text"": ""word1"" },
      { ""id"": ""t-2"", ""text"": ""word2"" },
      { ""id"": ""t-3"", ""text"": ""word3"" }
    ]
  }
- grading: {
    ""validSequences"": [
      [""t-1"", ""t-2"", ""t-3""]
    ],
    ""explanation"": ""Grammar explanation...""
  }");
                    break;

                case "multiplechoice":
                    sb.AppendLine(@"### MultipleChoice:
- type: ""MultipleChoice""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""question"": ""Question statement or fill-in prompt?"",
    ""selectionMode"": ""Single"", // or ""Multiple""
    ""options"": [
      { ""id"": ""opt-a"", ""text"": ""Option A"" },
      { ""id"": ""opt-b"", ""text"": ""Option B"" },
      { ""id"": ""opt-c"", ""text"": ""Option C"" },
      { ""id"": ""opt-d"", ""text"": ""Option D"" }
    ]
  }
- grading: {
    ""correctOptionIds"": [""opt-b""],
    ""explanation"": ""Why this option is correct...""
  }");
                    break;

                case "fillintheblanks":
                    sb.AppendLine(@"### FillInTheBlanks:
- type: ""FillInTheBlanks""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""textWithBlanks"": ""Text containing [blank-1] and [blank-2]."",
    ""blanks"": [
      { ""id"": ""blank-1"", ""hint"": ""first clue"", ""maxLength"": 20 },
      { ""id"": ""blank-2"", ""hint"": ""second clue"", ""maxLength"": 20 }
    ]
  }
- grading: {
    ""isOpenEnded"": false,
    ""blankAnswers"": {
      ""blank-1"": { ""acceptedAnswers"": [""word1"", ""variant1""], ""caseSensitive"": false },
      ""blank-2"": { ""acceptedAnswers"": [""word2""], ""caseSensitive"": false }
    },
    ""explanation"": ""Vocabulary and grammar notes...""
  }");
                    break;

                case "flashcards":
                    sb.AppendLine(@"### Flashcards:
- type: ""Flashcards""
- maxPoints: 0
- gradingMode: ""SelfAssessment""
- content: {
    ""cards"": [
      {
        ""id"": ""c-1"",
        ""term"": ""English word"",
        ""ipa"": ""/phonetics/"",
        ""meaning"": ""Vietnamese definition"",
        ""example"": ""Example sentence."",
        ""audioAssetId"": null
      }
    ]
  }
- grading: {}");
                    break;

                case "matching":
                    sb.AppendLine(@"### Matching:
- type: ""Matching""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""leftItems"": [
      { ""id"": ""l-1"", ""text"": ""Phrase 1"" },
      { ""id"": ""l-2"", ""text"": ""Phrase 2"" }
    ],
    ""rightItems"": [
      { ""id"": ""r-1"", ""text"": ""Meaning 1"" },
      { ""id"": ""r-2"", ""text"": ""Meaning 2"" }
    ]
  }
- grading: {
    ""correctPairs"": [
      { ""leftId"": ""l-1"", ""rightId"": ""r-1"" },
      { ""leftId"": ""l-2"", ""rightId"": ""r-2"" }
    ],
    ""explanation"": ""Matching explanation...""
  }");
                    break;

                case "translation":
                    sb.AppendLine(@"### Translation:
- type: ""Translation""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""direction"": ""ViToEn"", // or ""EnToVi""
    ""sourceText"": ""Câu tiếng Việt cần dịch..."",
    ""context"": ""Ngữ cảnh hội thoại hoặc chủ đề""
  }
- grading: {
    ""rubricCode"": ""LESSON_TRANSLATION"",
    ""rubricVersion"": 1,
    ""referenceTranslations"": [""Natural English sentence 1"", ""Alternative English sentence 2""],
    ""requiredKeyIdeas"": [""Key concept 1"", ""Key concept 2""]
  }");
                    break;

                case "truefalse":
                    sb.AppendLine(@"### TrueFalse:
- type: ""TrueFalse""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""context"": ""Context paragraph..."",
    ""statements"": [
      { ""id"": ""s-1"", ""text"": ""Statement 1"" },
      { ""id"": ""s-2"", ""text"": ""Statement 2"" }
    ]
  }
- grading: {
    ""statementAnswers"": { ""s-1"": true, ""s-2"": false },
    ""explanations"": { ""s-1"": ""Reason 1..."", ""s-2"": ""Reason 2..."" }
  }");
                    break;

                case "errorcorrection":
                    sb.AppendLine(@"### ErrorCorrection:
- type: ""ErrorCorrection""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""incorrectSentence"": ""Sentence with a grammatical error."",
    ""instructions"": ""Find and correct the error.""
  }
- grading: {
    ""referenceCorrections"": [""Corrected sentence."", ""Alternative correct sentence.""],
    ""targetErrors"": [""Description of the error""],
    ""rubricCode"": ""LESSON_ERROR_CORRECTION"",
    ""rubricVersion"": 1
  }");
                    break;

                case "spellingpractice":
                    sb.AppendLine(@"### SpellingPractice:
- type: ""SpellingPractice""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""hint"": ""Clue or definition of the target word"",
    ""letterCount"": 8
  }
- grading: {
    ""targetWord"": ""targetword"",
    ""acceptedVariants"": [""variant""],
    ""caseSensitive"": false
  }");
                    break;

                case "speakingpractice":
                    sb.AppendLine(@"### SpeakingPractice:
- type: ""SpeakingPractice""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""topic"": ""Speaking topic title"",
    ""instructions"": ""Speak for at least 30 seconds..."",
    ""targetDurationSeconds"": 30,
    ""suggestedPrompts"": [""Guiding question 1"", ""Guiding question 2""]
  }
- grading: {
    ""rubricCode"": ""LESSON_SPEAKING"",
    ""rubricVersion"": 1,
    ""taskInstructions"": ""Teacher assessment instructions...""
  }");
                    break;

                case "readingcomprehension":
                    sb.AppendLine(@"### ReadingComprehension:
- type: ""ReadingComprehension""
- maxPoints: 10
- gradingMode: ""Composite""
- content: {
    ""passage"": ""Passage text..."",
    ""subQuestions"": [
      {
        ""id"": ""q-1"",
        ""questionKind"": ""SingleChoice"",
        ""order"": 1,
        ""maxPoints"": 5,
        ""questionText"": ""Question?"",
        ""options"": [{ ""id"": ""o-1"", ""text"": ""A"" }, { ""id"": ""o-2"", ""text"": ""B"" }],
        ""grading"": { ""correctOptionIds"": [""o-1""], ""explanation"": ""..."" }
      }
    ]
  }
- grading: {}");
                    break;

                case "listeningcomprehension":
                    sb.AppendLine(@"### ListeningComprehension:
- type: ""ListeningComprehension""
- maxPoints: 10
- gradingMode: ""Composite""
- content: {
    ""audioAssetId"": null,
    ""transcript"": ""Audio transcript text..."",
    ""subQuestions"": [
      {
        ""id"": ""q-1"",
        ""questionKind"": ""SingleChoice"",
        ""order"": 1,
        ""maxPoints"": 10,
        ""questionText"": ""Question?"",
        ""options"": [{ ""id"": ""o-1"", ""text"": ""A"" }, { ""id"": ""o-2"", ""text"": ""B"" }],
        ""grading"": { ""correctOptionIds"": [""o-1""], ""explanation"": ""..."" }
      }
    ]
  }
- grading: {}");
                    break;

                case "dictation":
                    sb.AppendLine(@"### Dictation:
- type: ""Dictation""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""audioAssetId"": null,
    ""hint"": ""Listen and type what you hear.""
  }
- grading: {
    ""referenceTranscript"": ""Expected transcript sentence."",
    ""caseSensitive"": false,
    ""ignorePunctuation"": true
  }");
                    break;

                case "shadowing":
                    sb.AppendLine(@"### Shadowing:
- type: ""Shadowing""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""audioAssetId"": null,
    ""transcript"": ""Sentence to speak aloud simultaneously."",
    ""speechRateWpm"": 120
  }
- grading: {
    ""rubricCode"": ""LESSON_SHADOWING"",
    ""rubricVersion"": 1
  }");
                    break;

                case "conversationroleplay":
                    sb.AppendLine(@"### ConversationRolePlay:
- type: ""ConversationRolePlay""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""scenario"": ""Role-play scenario..."",
    ""aiRole"": ""AI Persona"",
    ""learnerRole"": ""Learner Persona"",
    ""initialGreeting"": ""AI greeting"",
    ""minTurns"": 3,
    ""maxTurns"": 6
  }
- grading: {
    ""rubricCode"": ""LESSON_ROLEPLAY"",
    ""rubricVersion"": 1
  }");
                    break;

                case "writingpractice":
                    sb.AppendLine(@"### WritingPractice:
- type: ""WritingPractice""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""prompt"": ""Writing prompt..."",
    ""minWords"": 40,
    ""maxWords"": 100
  }
- grading: {
    ""rubricCode"": ""LESSON_WRITING"",
    ""rubricVersion"": 1,
    ""guidelines"": ""Grammar and vocabulary focus...""
  }");
                    break;
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildConcreteExerciseExamples(IReadOnlyList<string> types, string topic, string cefrLevel)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        int order = 1;

        for (int i = 0; i < uniqueTypes.Count; i++)
        {
            string t = uniqueTypes[i].ToLowerInvariant();
            string jsonChunk = t switch
            {
                "sentenceunscramble" => $@"    {{
      ""key"": ""ex_{order:D2}_unscramble"",
      ""type"": ""SentenceUnscramble"",
      ""title"": ""Sentence Unscramble: {topic}"",
      ""instructions"": ""Rearrange the tokens into a grammatically correct sentence."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""prompt"": ""Rearrange words to form a correct sentence:"",
        ""tokens"": [
          {{ ""id"": ""t-1"", ""text"": ""I"" }},
          {{ ""id"": ""t-2"", ""text"": ""usually"" }},
          {{ ""id"": ""t-3"", ""text"": ""start"" }},
          {{ ""id"": ""t-4"", ""text"": ""work"" }},
          {{ ""id"": ""t-5"", ""text"": ""at"" }},
          {{ ""id"": ""t-6"", ""text"": ""eight."" }}
        ]
      }},
      ""grading"": {{
        ""validSequences"": [
          [""t-1"", ""t-2"", ""t-3"", ""t-4"", ""t-5"", ""t-6""]
        ],
        ""explanation"": ""Subject + frequency adverb + verb phrase + time: 'I usually start work at eight.'""
      }}
    }}",

                "multiplechoice" => $@"    {{
      ""key"": ""ex_{order:D2}_mcq"",
      ""type"": ""MultipleChoice"",
      ""title"": ""Multiple Choice: {topic}"",
      ""instructions"": ""Select the correct option to complete the statement."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""question"": ""Which sentence correctly describes a good {topic} habit?"",
        ""selectionMode"": ""Single"",
        ""options"": [
          {{ ""id"": ""opt-a"", ""text"": ""I takes a break every hour."" }},
          {{ ""id"": ""opt-b"", ""text"": ""I take a break every hour."" }},
          {{ ""id"": ""opt-c"", ""text"": ""I am take a break every hour."" }},
          {{ ""id"": ""opt-d"", ""text"": ""I took breaks tomorrow."" }}
        ]
      }},
      ""grading"": {{
        ""correctOptionIds"": [""opt-b""],
        ""explanation"": ""'I' takes the base verb form 'take' in the present simple tense.""
      }}
    }}",

                "fillintheblanks" => $@"    {{
      ""key"": ""ex_{order:D2}_fill"",
      ""type"": ""FillInTheBlanks"",
      ""title"": ""Fill in the Blanks: {topic}"",
      ""instructions"": ""Type the appropriate word into each blank."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""textWithBlanks"": ""Every morning, I [blank-1] up at seven and [blank-2] breakfast before working."",
        ""blanks"": [
          {{ ""id"": ""blank-1"", ""hint"": ""wake / get"", ""maxLength"": 20 }},
          {{ ""id"": ""blank-2"", ""hint"": ""eat / have"", ""maxLength"": 20 }}
        ]
      }},
      ""grading"": {{
        ""isOpenEnded"": false,
        ""blankAnswers"": {{
          ""blank-1"": {{ ""acceptedAnswers"": [""wake"", ""get""], ""caseSensitive"": false }},
          ""blank-2"": {{ ""acceptedAnswers"": [""have"", ""eat"", ""make""], ""caseSensitive"": false }}
        }},
        ""explanation"": ""Common collocations: 'wake up' / 'get up' and 'have/eat breakfast'.""
      }}
    }}",

                "flashcards" => $@"    {{
      ""key"": ""ex_{order:D2}_vocab"",
      ""type"": ""Flashcards"",
      ""title"": ""Vocabulary Flashcards: {topic}"",
      ""instructions"": ""Review key vocabulary, definitions, and IPA pronunciation."",
      ""order"": {order},
      ""maxPoints"": 0,
      ""gradingMode"": ""SelfAssessment"",
      ""content"": {{
        ""cards"": [
          {{
            ""id"": ""c-1"",
            ""term"": ""commute"",
            ""ipa"": ""/kəˈmjuːt/"",
            ""meaning"": ""quãng đường đi lại hàng ngày"",
            ""example"": ""Working from home eliminates daily commute time."",
            ""audioAssetId"": null
          }},
          {{
            ""id"": ""c-2"",
            ""term"": ""work-life balance"",
            ""ipa"": ""/ˌwɜːk.laɪf ˈbæl.əns/"",
            ""meaning"": ""cân bằng công việc và đời sống"",
            ""example"": ""Maintaining a consistent routine improves work-life balance."",
            ""audioAssetId"": null
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "matching" => $@"    {{
      ""key"": ""ex_{order:D2}_matching"",
      ""type"": ""Matching"",
      ""title"": ""Vocabulary Matching: {topic}"",
      ""instructions"": ""Match each action on the left with its definition on the right."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""leftItems"": [
          {{ ""id"": ""l-1"", ""text"": ""take a break"" }},
          {{ ""id"": ""l-2"", ""text"": ""attend a meeting"" }}
        ],
        ""rightItems"": [
          {{ ""id"": ""r-1"", ""text"": ""stop working briefly to rest"" }},
          {{ ""id"": ""r-2"", ""text"": ""join colleagues for discussion"" }}
        ]
      }},
      ""grading"": {{
        ""correctPairs"": [
          {{ ""leftId"": ""l-1"", ""rightId"": ""r-1"" }},
          {{ ""leftId"": ""l-2"", ""rightId"": ""r-2"" }}
        ],
        ""explanation"": ""'Take a break' means to rest briefly; 'Attend a meeting' means to join colleagues.""
      }}
    }}",

                "translation" => $@"    {{
      ""key"": ""ex_{order:D2}_translation"",
      ""type"": ""Translation"",
      ""title"": ""Translation: {topic}"",
      ""instructions"": ""Translate the Vietnamese sentence into natural English."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""direction"": ""ViToEn"",
        ""sourceText"": ""Tôi thường làm việc từ xa ba ngày một tuần để cân bằng cuộc sống."",
        ""context"": ""Talking about weekly schedule in {topic}""
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_TRANSLATION"",
        ""rubricVersion"": 1,
        ""referenceTranslations"": [
          ""I usually work remotely three days a week to balance my life."",
          ""I often work from home three days a week for work-life balance.""
        ],
        ""requiredKeyIdeas"": [
          ""Adverb 'usually' / 'often' with present simple"",
          ""Work remotely / work from home"",
          ""Three days a week / three days per week""
        ]
      }}
    }}",

                "truefalse" => $@"    {{
      ""key"": ""ex_{order:D2}_truefalse"",
      ""type"": ""TrueFalse"",
      ""title"": ""True or False: {topic}"",
      ""instructions"": ""Read the context and decide whether each statement is True or False."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""context"": ""Alex is a software engineer who works remotely. He wakes up at 7:00 AM, exercises for 30 minutes, and starts coding at 8:30 AM. He shuts down his computer by 6:00 PM."",
        ""statements"": [
          {{ ""id"": ""s-1"", ""text"": ""Alex exercises before he starts coding."" }},
          {{ ""id"": ""s-2"", ""text"": ""Alex continues coding until late at night."" }}
        ]
      }},
      ""grading"": {{
        ""statementAnswers"": {{
          ""s-1"": true,
          ""s-2"": false
        }},
        ""explanations"": {{
          ""s-1"": ""True: Alex exercises for 30 minutes before starting coding at 8:30 AM."",
          ""s-2"": ""False: He shuts down his computer by 6:00 PM.""
        }}
      }}
    }}",

                "errorcorrection" => $@"    {{
      ""key"": ""ex_{order:D2}_error"",
      ""type"": ""ErrorCorrection"",
      ""title"": ""Error Correction: {topic}"",
      ""instructions"": ""Find the grammatical error and type the corrected sentence."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""incorrectSentence"": ""He don't like attending online meetings in the morning."",
        ""instructions"": ""Correct the subject-verb agreement mistake.""
      }},
      ""grading"": {{
        ""referenceCorrections"": [
          ""He doesn't like attending online meetings in the morning."",
          ""He does not like attending online meetings in the morning.""
        ],
        ""targetErrors"": [""Subject-verb agreement: 'don't' -> 'doesn't'""],
        ""rubricCode"": ""LESSON_ERROR_CORRECTION"",
        ""rubricVersion"": 1
      }}
    }}",

                "spellingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_spelling"",
      ""type"": ""SpellingPractice"",
      ""title"": ""Spelling Practice: {topic}"",
      ""instructions"": ""Spell the English word correctly based on the clue."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""hint"": ""A plan of activities or tasks and the times when they will happen"",
        ""letterCount"": 8
      }},
      ""grading"": {{
        ""targetWord"": ""schedule"",
        ""acceptedVariants"": [""calendar""],
        ""caseSensitive"": false
      }}
    }}",

                "speakingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_speaking"",
      ""type"": ""SpeakingPractice"",
      ""title"": ""Speaking Practice: {topic}"",
      ""instructions"": ""Speak clearly for at least 30 seconds answering the prompts."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""topic"": ""Describe your typical daily schedule"",
        ""instructions"": ""Speak for 30 to 60 seconds about your morning habits and workday."",
        ""targetDurationSeconds"": 30,
        ""suggestedPrompts"": [
          ""What time do you usually get up?"",
          ""How do you start your working day?"",
          ""What is your favorite habit to relax in the evening?""
        ]
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_SPEAKING"",
        ""rubricVersion"": 1,
        ""taskInstructions"": ""Assess present simple usage, time vocabulary, and clear pronunciation.""
      }}
    }}",

                "readingcomprehension" => $@"    {{
      ""key"": ""ex_{order:D2}_reading"",
      ""type"": ""ReadingComprehension"",
      ""title"": ""Reading Comprehension: {topic}"",
      ""instructions"": ""Read the passage carefully and answer the questions that follow."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Composite"",
      ""content"": {{
        ""passage"": ""Remote work offers flexibility, but structuring your daily routine is essential. Many software engineers begin by establishing regular working hours and taking short breaks to maintain productivity and prevent burnout."",
        ""subQuestions"": [
          {{
            ""id"": ""q-1"",
            ""questionKind"": ""SingleChoice"",
            ""order"": 1,
            ""maxPoints"": 5,
            ""questionText"": ""What helps remote software engineers prevent burnout?"",
            ""options"": [
              {{ ""id"": ""opt-1"", ""text"": ""Taking regular short breaks"" }},
              {{ ""id"": ""opt-2"", ""text"": ""Working non-stop through the evening"" }}
            ],
            ""grading"": {{
              ""correctOptionIds"": [""opt-1""],
              ""explanation"": ""Taking short breaks prevents burnout and maintains productivity.""
            }}
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "listeningcomprehension" => $@"    {{
      ""key"": ""ex_{order:D2}_listening"",
      ""type"": ""ListeningComprehension"",
      ""title"": ""Listening Comprehension: {topic}"",
      ""instructions"": ""Listen to the recording and answer the comprehension questions."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Composite"",
      ""content"": {{
        ""audioAssetId"": null,
        ""transcript"": ""Hi team, today we need to coordinate on the new deployment schedule. I suggest we conduct our daily standup at 9:30 AM so everyone has enough time to prepare."",
        ""subQuestions"": [
          {{
            ""id"": ""lq-1"",
            ""questionKind"": ""SingleChoice"",
            ""order"": 1,
            ""maxPoints"": 10,
            ""questionText"": ""What time does the speaker suggest holding the daily standup?"",
            ""options"": [
              {{ ""id"": ""opt-1"", ""text"": ""At 9:30 AM"" }},
              {{ ""id"": ""opt-2"", ""text"": ""At 8:00 AM"" }}
            ],
            ""grading"": {{
              ""correctOptionIds"": [""opt-1""],
              ""explanation"": ""The speaker states: 'I suggest we conduct our daily standup at 9:30 AM.'""
            }}
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "dictation" => $@"    {{
      ""key"": ""ex_{order:D2}_dictation"",
      ""type"": ""Dictation"",
      ""title"": ""Dictation: {topic}"",
      ""instructions"": ""Listen to the audio sentence and type exactly what you hear."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""audioAssetId"": null,
        ""hint"": ""A sentence about daily remote work routines.""
      }},
      ""grading"": {{
        ""referenceTranscript"": ""I usually organize my schedule before starting morning tasks."",
        ""caseSensitive"": false,
        ""ignorePunctuation"": true
      }}
    }}",

                "shadowing" => $@"    {{
      ""key"": ""ex_{order:D2}_shadowing"",
      ""type"": ""Shadowing"",
      ""title"": ""Shadowing: {topic}"",
      ""instructions"": ""Listen carefully and repeat the sentence aloud with natural pronunciation, pacing, and intonation."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""audioAssetId"": null,
        ""transcript"": ""I establish clear boundaries between my working hours and personal time."",
        ""speechRateWpm"": 120
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_SHADOWING"",
        ""rubricVersion"": 1
      }}
    }}",

                "conversationroleplay" => $@"    {{
      ""key"": ""ex_{order:D2}_roleplay"",
      ""type"": ""ConversationRolePlay"",
      ""title"": ""Role Play: {topic}"",
      ""instructions"": ""Have a conversation with your AI colleague about work-life balance and daily routines."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""scenario"": ""You and a remote teammate are discussing daily habits, work schedule, and taking breaks during remote work."",
        ""aiRole"": ""Supportive Senior Colleague"",
        ""learnerRole"": ""Remote Team Member"",
        ""initialGreeting"": ""Hi! How is your daily remote routine going this week? Do you find enough time to take breaks?"",
        ""minTurns"": 3,
        ""maxTurns"": 6
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_ROLEPLAY"",
        ""rubricVersion"": 1
      }}
    }}",

                "writingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_writing"",
      ""type"": ""WritingPractice"",
      ""title"": ""Short Writing: {topic}"",
      ""instructions"": ""Write a short paragraph (40-80 words) describing your daily schedule or remote work habit."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""prompt"": ""Describe your typical daily routine when working or studying from home, including when you start and how you relax."",
        ""minWords"": 40,
        ""maxWords"": 80
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_WRITING"",
        ""rubricVersion"": 1
      }}
    }}",

                _ => $@"    {{
      ""key"": ""ex_{order:D2}_item"",
      ""type"": ""{(t.Length > 0 ? char.ToUpperInvariant(t[0]) + (t.Length > 1 ? t[1..] : "") : "Exercise")}"",
      ""title"": ""Exercise: {topic}"",
      ""instructions"": ""Complete the exercise."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{}},
      ""grading"": {{}}
    }}"
            };

            sb.Append(jsonChunk);
            if (i < uniqueTypes.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }

            order++;
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }
}
