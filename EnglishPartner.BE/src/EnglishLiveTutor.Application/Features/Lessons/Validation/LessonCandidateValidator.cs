using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Validation;

public sealed class LessonCandidateValidator : ILessonCandidateValidator
{
    private static readonly HashSet<string> ValidCefrLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1", "A2", "B1", "B2", "C1", "C2"
    };

    private readonly IAppDbContext _db;

    public LessonCandidateValidator(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<LessonCandidateValidationResultDto> ValidateCandidateAsync(JsonNode? candidateRoot, CancellationToken ct = default)
    {
        var issues = new List<ValidationIssueDto>();

        if (candidateRoot == null)
        {
            issues.Add(new ValidationIssueDto("$", "Error", "Candidate JSON cannot be null or empty."));
            return new LessonCandidateValidationResultDto(false, issues, null);
        }

        if (candidateRoot is not JsonObject rootObj)
        {
            issues.Add(new ValidationIssueDto("$", "Error", "Root element must be a JSON object."));
            return new LessonCandidateValidationResultDto(false, issues, candidateRoot);
        }

        // 1. Schema version
        string schemaVersion = rootObj["schemaVersion"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            issues.Add(new ValidationIssueDto("$.schemaVersion", "Warning", "Missing schemaVersion field; expected 'lesson-exercises.v1'."));
        }
        else if (!string.Equals(schemaVersion, "lesson-exercises.v1", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssueDto("$.schemaVersion", "Error", $"Unsupported schemaVersion '{schemaVersion}'. Expected 'lesson-exercises.v1'."));
        }

        // 2. Lesson metadata
        if (rootObj["lesson"] is not JsonObject lessonNode)
        {
            issues.Add(new ValidationIssueDto("$.lesson", "Error", "Field 'lesson' metadata object is required."));
        }
        else
        {
            string title = lessonNode["title"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                issues.Add(new ValidationIssueDto("$.lesson.title", "Error", "Lesson title is required."));

            string cefr = lessonNode["cefrLevel"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cefr))
                issues.Add(new ValidationIssueDto("$.lesson.cefrLevel", "Error", "CEFR level is required."));
            else if (!ValidCefrLevels.Contains(cefr))
                issues.Add(new ValidationIssueDto("$.lesson.cefrLevel", "Error", $"Invalid CEFR level '{cefr}'. Must be one of: A1, A2, B1, B2, C1, C2."));

            string topic = lessonNode["topic"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(topic))
                issues.Add(new ValidationIssueDto("$.lesson.topic", "Error", "Lesson topic is required."));
        }

        // 3. Exercises array
        var rubricCodesToVerify = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (rootObj["exercises"] is not JsonArray exercisesArray || exercisesArray.Count == 0)
        {
            issues.Add(new ValidationIssueDto("$.exercises", "Error", "At least one exercise is required in the exercises array."));
        }
        else
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < exercisesArray.Count; i++)
            {
                string path = $"$.exercises[{i}]";
                if (exercisesArray[i] is not JsonObject ex)
                {
                    issues.Add(new ValidationIssueDto(path, "Error", "Exercise entry must be a JSON object."));
                    continue;
                }

                // Exercise key
                string key = ex["key"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                {
                    issues.Add(new ValidationIssueDto($"{path}.key", "Error", "Exercise key is required."));
                }
                else if (!keys.Add(key))
                {
                    issues.Add(new ValidationIssueDto($"{path}.key", "Error", $"Duplicate exercise key '{key}'."));
                }

                // Exercise type
                string typeStr = ex["type"]?.ToString() ?? string.Empty;
                if (!Enum.TryParse<ExerciseType>(typeStr, ignoreCase: true, out var exerciseType))
                {
                    issues.Add(new ValidationIssueDto($"{path}.type", "Error", $"Invalid or unrecognized ExerciseType '{typeStr}'."));
                    continue;
                }

                // Content
                if (ex["content"] is not JsonObject content)
                {
                    issues.Add(new ValidationIssueDto($"{path}.content", "Error", "Exercise content object is required."));
                    continue;
                }

                // Grading mode & MaxPoints
                string gradingModeStr = ex["gradingMode"]?.ToString() ?? string.Empty;
                GradingMode gradingMode = GradingMode.Deterministic;
                if (!string.IsNullOrWhiteSpace(gradingModeStr) && Enum.TryParse<GradingMode>(gradingModeStr, ignoreCase: true, out var parsedMode))
                {
                    gradingMode = parsedMode;
                }

                decimal maxPoints = 0;
                if (ex["maxPoints"] is JsonValue maxPointsVal && maxPointsVal.TryGetValue<decimal>(out var parsedMaxPoints))
                {
                    maxPoints = parsedMaxPoints;
                }

                if (gradingMode == GradingMode.SelfAssessment)
                {
                    if (maxPoints != 0)
                    {
                        issues.Add(new ValidationIssueDto($"{path}.maxPoints", "Warning", "SelfAssessment exercises should have maxPoints set to 0."));
                    }
                }
                else if (maxPoints < 0)
                {
                    issues.Add(new ValidationIssueDto($"{path}.maxPoints", "Error", "maxPoints must be greater than or equal to 0."));
                }

                var grading = ex["grading"] as JsonObject;

                // Semantic validation per exercise type
                ValidateExerciseTypeSemantics(path, exerciseType, content, grading, issues, rubricCodesToVerify);
            }
        }

        // 4. Verify referenced rubric codes against database
        if (rubricCodesToVerify.Count > 0)
        {
            var codes = rubricCodesToVerify.Keys.ToList();
            var existingCodes = await _db.ScoringRubrics
                .AsNoTracking()
                .Where(r => codes.Contains(r.Code) && !r.IsDeleted)
                .Select(r => r.Code)
                .ToListAsync(ct);

            var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
            foreach (var (code, paths) in rubricCodesToVerify)
            {
                if (!existingSet.Contains(code))
                {
                    foreach (var p in paths)
                    {
                        issues.Add(new ValidationIssueDto(p, "Error", $"Scoring rubric '{code}' does not exist or is inactive."));
                    }
                }
            }
        }

        bool isValid = issues.All(i => i.Severity != "Error");
        return new LessonCandidateValidationResultDto(isValid, issues, candidateRoot);
    }

    private static void ValidateExerciseTypeSemantics(
        string path,
        ExerciseType type,
        JsonObject content,
        JsonObject? grading,
        List<ValidationIssueDto> issues,
        Dictionary<string, List<string>> rubricCodesToVerify)
    {
        switch (type)
        {
            case ExerciseType.Flashcards:
                if (content["cards"] is not JsonArray)
                    issues.Add(new ValidationIssueDto($"{path}.content.cards", "Error", "Flashcards exercise requires a 'cards' array."));
                break;

            case ExerciseType.MultipleChoice:
                if (content["options"] is not JsonArray options || options.Count < 2)
                {
                    issues.Add(new ValidationIssueDto($"{path}.content.options", "Error", "MultipleChoice requires at least 2 options."));
                }
                else
                {
                    var optIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var opt in options)
                    {
                        string optId = opt?["id"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(optId)) optIds.Add(optId);
                    }

                    if (grading != null && grading["correctOptionIds"] is JsonArray correctIds)
                    {
                        if (correctIds.Count == 0)
                            issues.Add(new ValidationIssueDto($"{path}.grading.correctOptionIds", "Error", "At least one correctOptionId must be specified."));

                        foreach (var cId in correctIds)
                        {
                            string idStr = cId?.ToString() ?? string.Empty;
                            if (!optIds.Contains(idStr))
                            {
                                issues.Add(new ValidationIssueDto($"{path}.grading.correctOptionIds", "Error", $"correctOptionId '{idStr}' does not match any option in content.options."));
                            }
                        }

                        string selMode = content["selectionMode"]?.ToString() ?? string.Empty;
                        if (string.Equals(selMode, "Single", StringComparison.OrdinalIgnoreCase) && correctIds.Count > 1)
                        {
                            issues.Add(new ValidationIssueDto($"{path}.content.selectionMode", "Error", $"MultipleChoice selectionMode is 'Single' but {correctIds.Count} correctOptionIds were specified."));
                        }
                    }
                }
                break;

            case ExerciseType.FillInTheBlanks:
                string textWithBlanks = content["textWithBlanks"]?.ToString() ?? content["text"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(textWithBlanks))
                    issues.Add(new ValidationIssueDto($"{path}.content.textWithBlanks", "Error", "textWithBlanks is required for FillInTheBlanks."));
                if (content["blanks"] is not JsonArray blanks || blanks.Count == 0)
                    issues.Add(new ValidationIssueDto($"{path}.content.blanks", "Error", "At least one blank must be defined in content.blanks."));
                break;

            case ExerciseType.SentenceUnscramble:
                if (content["tokens"] is not JsonArray tokens || tokens.Count < 2)
                {
                    issues.Add(new ValidationIssueDto($"{path}.content.tokens", "Error", "SentenceUnscramble requires at least 2 tokens."));
                }
                else
                {
                    var tokenIds = new HashSet<string>(tokens.Select(t => t?["id"]?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)), StringComparer.OrdinalIgnoreCase);
                    if (grading != null && grading["validSequences"] is JsonArray seqs)
                    {
                        foreach (var seq in seqs)
                        {
                            if (seq is JsonArray innerSeq)
                            {
                                foreach (var tId in innerSeq)
                                {
                                    string idStr = tId?.ToString() ?? string.Empty;
                                    if (!tokenIds.Contains(idStr))
                                    {
                                        issues.Add(new ValidationIssueDto($"{path}.grading.validSequences", "Error", $"Token ID '{idStr}' in validSequences does not exist in content.tokens."));
                                    }
                                }
                            }
                        }
                    }
                }
                break;

            case ExerciseType.Matching:
                if (content["leftItems"] is not JsonArray leftItems || leftItems.Count == 0)
                    issues.Add(new ValidationIssueDto($"{path}.content.leftItems", "Error", "Matching requires leftItems array."));
                if (content["rightItems"] is not JsonArray rightItems || rightItems.Count == 0)
                    issues.Add(new ValidationIssueDto($"{path}.content.rightItems", "Error", "Matching requires rightItems array."));

                if (grading != null)
                {
                    var pairsArr = grading["correctPairs"] as JsonArray ?? grading["pairs"] as JsonArray;
                    if (pairsArr == null || pairsArr.Count == 0)
                    {
                        issues.Add(new ValidationIssueDto($"{path}.grading.correctPairs", "Error", "Matching requires correctPairs array with at least one pair."));
                    }
                    else
                    {
                        var leftIds = content["leftItems"] is JsonArray lArr
                            ? lArr.Select(x => x?["id"]?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var rightIds = content["rightItems"] is JsonArray rArr
                            ? rArr.Select(x => x?["id"]?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var p in pairsArr)
                        {
                            string lId = p?["leftId"]?.ToString() ?? "";
                            string rId = p?["rightId"]?.ToString() ?? "";
                            if (leftIds.Count > 0 && !leftIds.Contains(lId))
                                issues.Add(new ValidationIssueDto($"{path}.grading.correctPairs", "Error", $"leftId '{lId}' does not match any item in content.leftItems."));
                            if (rightIds.Count > 0 && !rightIds.Contains(rId))
                                issues.Add(new ValidationIssueDto($"{path}.grading.correctPairs", "Error", $"rightId '{rId}' does not match any item in content.rightItems."));
                        }
                    }
                }
                break;

            case ExerciseType.SpellingPractice:
                string tWord = grading?["targetWord"]?.ToString() ?? grading?["correctAnswer"]?.ToString() ?? "";
                var accSpellings = grading?["acceptedSpellings"] as JsonArray ?? grading?["acceptedAnswers"] as JsonArray ?? grading?["acceptedVariants"] as JsonArray;
                if (string.IsNullOrWhiteSpace(tWord) && (accSpellings == null || accSpellings.Count == 0))
                {
                    issues.Add(new ValidationIssueDto($"{path}.grading.targetWord", "Error", "SpellingPractice requires targetWord or acceptedSpellings."));
                }
                break;

            case ExerciseType.TrueFalse:
                if (content["statements"] is not JsonArray stmts || stmts.Count == 0)
                {
                    issues.Add(new ValidationIssueDto($"{path}.content.statements", "Error", "TrueFalse requires statements array."));
                }
                else if (grading != null)
                {
                    var stmtAnswers = grading["statementAnswers"] as JsonObject ?? grading["answers"] as JsonObject;
                    if (stmtAnswers == null || stmtAnswers.Count == 0)
                    {
                        issues.Add(new ValidationIssueDto($"{path}.grading.statementAnswers", "Error", "TrueFalse requires statementAnswers mapping."));
                    }
                    else
                    {
                        foreach (var s in stmts)
                        {
                            string sId = s?["id"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(sId) && !stmtAnswers.ContainsKey(sId))
                            {
                                issues.Add(new ValidationIssueDto($"{path}.grading.statementAnswers", "Error", $"Statement '{sId}' is missing an answer in statementAnswers."));
                            }
                        }
                    }
                }
                break;

            case ExerciseType.Dictation:
                string refTranscript = grading?["referenceTranscript"]?.ToString() ?? grading?["transcript"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(refTranscript))
                    issues.Add(new ValidationIssueDto($"{path}.grading.referenceTranscript", "Error", "Dictation requires referenceTranscript in grading definition."));
                break;

            case ExerciseType.Translation:
            case ExerciseType.SpeakingPractice:
            case ExerciseType.Shadowing:
            case ExerciseType.ConversationRolePlay:
            case ExerciseType.WritingPractice:
                // Check rubric code if present
                string? rubricCode = grading?["rubricCode"]?.ToString();
                if (!string.IsNullOrWhiteSpace(rubricCode))
                {
                    if (rubricCode is "LESSON_ROLEPLAY" or "LESSON_CONVERSATION")
                    {
                        rubricCode = "LESSON_CONVERSATION_TEXT";
                    }

                    if (!rubricCodesToVerify.TryGetValue(rubricCode, out var paths))
                    {
                        paths = new List<string>();
                        rubricCodesToVerify[rubricCode] = paths;
                    }
                    paths.Add($"{path}.grading.rubricCode");
                }
                break;

            default:
                break;
        }
    }
}
