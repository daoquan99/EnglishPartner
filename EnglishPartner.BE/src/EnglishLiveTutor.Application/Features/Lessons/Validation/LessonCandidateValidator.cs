using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exercises;
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
        var semanticIssues = ExerciseContentValidator.ValidateExerciseTypeSemantics(path, type, content, grading, rubricCodesToVerify);
        issues.AddRange(semanticIssues);
    }
}
