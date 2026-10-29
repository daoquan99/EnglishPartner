using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Exercises;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Rules;

public static class GrammarExerciseRules
{
    private static readonly Regex BlankMarkerRegex = new(@"\[(blank-[^\]\s]+)\]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceNormalizeRegex = new(@"\s+", RegexOptions.Compiled);

    public static readonly IReadOnlyList<ExerciseType> AllowedTypes =
    [
        ExerciseType.TrueFalse,
        ExerciseType.MultipleChoice,
        ExerciseType.Matching,
        ExerciseType.FillInTheBlanks,
        ExerciseType.SentenceUnscramble,
        ExerciseType.ErrorCorrection,
        ExerciseType.Translation,
        ExerciseType.WritingPractice,
        ExerciseType.ReadingComprehension
    ];

    public static readonly IReadOnlyList<ExerciseType> DeterministicTypes =
    [
        ExerciseType.TrueFalse,
        ExerciseType.MultipleChoice,
        ExerciseType.Matching,
        ExerciseType.FillInTheBlanks,
        ExerciseType.SentenceUnscramble,
        ExerciseType.ErrorCorrection
    ];

    private static readonly HashSet<string> ForbiddenAnswerKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "acceptedAnswers",
        "answer",
        "answers",
        "correctAnswer",
        "correctAnswers",
        "correctOptionIds",
        "statementAnswers",
        "correctPairs",
        "validSequences",
        "referenceCorrections",
        "acceptedCorrections",
        "referenceTranslations",
        "suggestedAnswer",
        "explanation",
        "targetWord"
    };

    public static List<ValidationIssueDto> Validate(
        ExerciseType type,
        string contentJson,
        string? gradingJson,
        decimal maxPoints,
        GradingMode gradingMode)
    {
        var issues = new List<ValidationIssueDto>();

        // 1. Allowed exercise types
        if (!AllowedTypes.Contains(type))
        {
            issues.Add(new ValidationIssueDto("type", "Error", $"ExerciseType '{type}' is not supported in Grammar Roadmap."));
            return issues;
        }

        // 2. MaxPoints
        if (maxPoints <= 0 || maxPoints > 100)
        {
            issues.Add(new ValidationIssueDto("maxPoints", "Error", "maxPoints must be greater than 0 and less than or equal to 100."));
        }

        // 3. ContentJson validation
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            issues.Add(new ValidationIssueDto("content", "Error", "ContentJson is required."));
            return issues;
        }

        if (contentJson.Length > 20000)
        {
            issues.Add(new ValidationIssueDto("content", "Error", "ContentJson must not exceed 20,000 characters."));
        }

        JsonObject? contentObj = null;
        try
        {
            var node = JsonNode.Parse(contentJson);
            if (node is JsonObject jObj)
            {
                contentObj = jObj;
            }
            else
            {
                issues.Add(new ValidationIssueDto("content", "Error", "ContentJson must be a valid JSON object."));
            }
        }
        catch (JsonException ex)
        {
            issues.Add(new ValidationIssueDto("content", "Error", $"Malformed ContentJson: {ex.Message}"));
        }

        // 4. GradingDefinitionJson validation
        JsonObject? gradingObj = null;
        if (!string.IsNullOrWhiteSpace(gradingJson))
        {
            if (gradingJson.Length > 20000)
            {
                issues.Add(new ValidationIssueDto("grading", "Error", "GradingDefinitionJson must not exceed 20,000 characters."));
            }

            try
            {
                var gNode = JsonNode.Parse(gradingJson);
                if (gNode is JsonObject gjObj)
                {
                    gradingObj = gjObj;
                }
                else
                {
                    issues.Add(new ValidationIssueDto("grading", "Error", "GradingDefinitionJson must be a valid JSON object."));
                }
            }
            catch (JsonException ex)
            {
                issues.Add(new ValidationIssueDto("grading", "Error", $"Malformed GradingDefinitionJson: {ex.Message}"));
            }
        }

        // Deterministic types MUST have grading definition
        bool isDeterministic = DeterministicTypes.Contains(type);
        if (isDeterministic)
        {
            if (gradingMode != GradingMode.Deterministic)
            {
                issues.Add(new ValidationIssueDto("gradingMode", "Error", $"{type} exercises require GradingMode 'Deterministic'."));
            }

            if (gradingObj == null)
            {
                issues.Add(new ValidationIssueDto("grading", "Error", $"GradingDefinitionJson is required for {type} exercises."));
            }
        }

        if (contentObj == null)
        {
            return issues;
        }

        // Validate content does not contain answer keys
        CheckForbiddenContentKeys(contentObj, type, issues);

        // 5. Shared semantics validator
        var semanticIssues = ExerciseContentValidator.ValidateExerciseTypeSemantics(
            "$",
            type,
            contentObj,
            gradingObj);
        issues.AddRange(semanticIssues);

        // 6. Detailed rules according to DeterministicExerciseGrader and Grammar spec
        switch (type)
        {
            case ExerciseType.MultipleChoice:
                ValidateMultipleChoice(contentObj, gradingObj, issues);
                break;

            case ExerciseType.TrueFalse:
                ValidateTrueFalse(contentObj, gradingObj, issues);
                break;

            case ExerciseType.Matching:
                ValidateMatching(contentObj, gradingObj, issues);
                break;

            case ExerciseType.FillInTheBlanks:
                ValidateFillInTheBlanks(contentObj, gradingObj, issues);
                break;

            case ExerciseType.SentenceUnscramble:
                ValidateSentenceUnscramble(contentObj, gradingObj, issues);
                break;

            case ExerciseType.ErrorCorrection:
                ValidateErrorCorrection(contentObj, gradingObj, issues);
                break;

            case ExerciseType.Translation:
                ValidateTranslation(contentObj, gradingObj, gradingMode, issues);
                break;

            case ExerciseType.WritingPractice:
                ValidateWritingPractice(contentObj, gradingObj, gradingMode, issues);
                break;

            case ExerciseType.ReadingComprehension:
                ValidateReadingComprehension(contentObj, gradingMode, issues);
                break;
        }

        return issues;
    }

    private static void CheckForbiddenContentKeys(JsonObject contentObj, ExerciseType type, List<ValidationIssueDto> issues)
    {
        ScanJson(contentObj, isReading: type == ExerciseType.ReadingComprehension, inSubQuestionItem: false);

        void ScanJson(JsonNode? node, bool isReading, bool inSubQuestionItem)
        {
            if (node == null) return;

            if (node is JsonObject obj)
            {
                foreach (var (key, value) in obj)
                {
                    if (isReading && inSubQuestionItem && string.Equals(key, "grading", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (ForbiddenAnswerKeys.Contains(key))
                    {
                        issues.Add(new ValidationIssueDto("content", "Error", $"Content must not contain answer key '{key}'."));
                    }

                    if (isReading && string.Equals(key, "subQuestions", StringComparison.OrdinalIgnoreCase) && value is JsonArray subQArr)
                    {
                        foreach (var subQ in subQArr)
                        {
                            ScanJson(subQ, isReading, inSubQuestionItem: true);
                        }
                    }
                    else
                    {
                        ScanJson(value, isReading, inSubQuestionItem: false);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    ScanJson(item, isReading, inSubQuestionItem);
                }
            }
        }
    }

    private static void ValidateMultipleChoice(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        if (gradingObj == null) return;

        var optIds = (contentObj["options"] as JsonArray)?
            .Select(x => x?["id"]?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var correctIdsArr = gradingObj["correctOptionIds"] as JsonArray;
        if (correctIdsArr == null || correctIdsArr.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.correctOptionIds", "Error", "MultipleChoice requires non-empty grading.correctOptionIds."));
            return;
        }

        var correctIds = correctIdsArr
            .Select(x => x?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        if (correctIds.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.correctOptionIds", "Error", "MultipleChoice requires at least one valid correctOptionId."));
            return;
        }

        foreach (var cId in correctIds)
        {
            if (!optIds.Contains(cId))
            {
                issues.Add(new ValidationIssueDto("grading.correctOptionIds", "Error", $"correctOptionId '{cId}' does not match any option in content.options."));
            }
        }

        string selectionMode = contentObj["selectionMode"]?.ToString() ?? string.Empty;
        if (string.Equals(selectionMode, "Single", StringComparison.OrdinalIgnoreCase) && correctIds.Count != 1)
        {
            issues.Add(new ValidationIssueDto("grading.correctOptionIds", "Error", $"MultipleChoice with selectionMode 'Single' must have exactly 1 correctOptionId, found {correctIds.Count}."));
        }
    }

    private static void ValidateTrueFalse(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        if (contentObj["statements"] is not JsonArray stmts || stmts.Count == 0)
        {
            issues.Add(new ValidationIssueDto("content.statements", "Error", "TrueFalse requires a non-empty statements array."));
            return;
        }

        if (gradingObj == null) return;

        var stmtAnswers = gradingObj["statementAnswers"] as JsonObject ?? gradingObj["answers"] as JsonObject;
        if (stmtAnswers == null || stmtAnswers.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.statementAnswers", "Error", "TrueFalse requires statementAnswers (or answers) mapping."));
            return;
        }

        foreach (var stmt in stmts)
        {
            string sId = stmt?["id"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sId))
            {
                issues.Add(new ValidationIssueDto("content.statements", "Error", "Each statement must have a non-empty 'id'."));
                continue;
            }

            if (!stmtAnswers.TryGetPropertyValue(sId, out var valNode) ||
                valNode is not JsonValue jVal ||
                !jVal.TryGetValue<bool>(out _))
            {
                issues.Add(new ValidationIssueDto("grading.statementAnswers", "Error", $"Statement '{sId}' must have a boolean answer in statementAnswers."));
            }
        }
    }

    private static void ValidateMatching(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        var leftIds = (contentObj["leftItems"] as JsonArray)?
            .Select(x => x?["id"]?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rightIds = (contentObj["rightItems"] as JsonArray)?
            .Select(x => x?["id"]?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (gradingObj == null) return;

        var pairsArr = gradingObj["correctPairs"] as JsonArray ?? gradingObj["pairs"] as JsonArray;
        if (pairsArr == null || pairsArr.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.correctPairs", "Error", "Matching requires non-empty correctPairs (or pairs) array."));
            return;
        }

        var seenLeftIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in pairsArr)
        {
            string leftId = p?["leftId"]?.ToString() ?? string.Empty;
            string rightId = p?["rightId"]?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(leftId) || !leftIds.Contains(leftId))
            {
                issues.Add(new ValidationIssueDto("grading.correctPairs", "Error", $"leftId '{leftId}' does not match any item in content.leftItems."));
            }

            if (string.IsNullOrWhiteSpace(rightId) || !rightIds.Contains(rightId))
            {
                issues.Add(new ValidationIssueDto("grading.correctPairs", "Error", $"rightId '{rightId}' does not match any item in content.rightItems."));
            }

            if (!string.IsNullOrWhiteSpace(leftId) && !seenLeftIds.Add(leftId))
            {
                issues.Add(new ValidationIssueDto("grading.correctPairs", "Error", $"leftId '{leftId}' is matched more than once. Each leftId must have at most one pair."));
            }
        }
    }

    private static void ValidateFillInTheBlanks(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        string textWithBlanks = contentObj["textWithBlanks"]?.ToString() ?? contentObj["text"]?.ToString() ?? string.Empty;
        var markerMatches = BlankMarkerRegex.Matches(textWithBlanks);
        var markerIds = markerMatches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var blanks = contentObj["blanks"] as JsonArray;
        var blankIds = blanks?
            .Select(b => b?["id"]?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (markerIds.Count == 0)
        {
            issues.Add(new ValidationIssueDto("content.textWithBlanks", "Error", "FillInTheBlanks requires at least one [blank-n] marker in textWithBlanks."));
        }

        if (blankIds.Count == 0)
        {
            issues.Add(new ValidationIssueDto("content.blanks", "Error", "FillInTheBlanks requires at least one blank in content.blanks."));
        }

        foreach (var mId in markerIds)
        {
            if (!blankIds.Contains(mId))
            {
                issues.Add(new ValidationIssueDto("content.textWithBlanks", "Error", $"Marker '[{mId}]' in textWithBlanks is missing from content.blanks."));
            }
        }

        foreach (var bId in blankIds)
        {
            if (!markerIds.Contains(bId))
            {
                issues.Add(new ValidationIssueDto("content.blanks", "Error", $"Blank '{bId}' in content.blanks does not appear in textWithBlanks."));
            }
        }

        if (gradingObj == null) return;

        var answersMap = ExtractFillInTheBlanksAnswers(gradingObj);
        if (answersMap == null || answersMap.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.blankAnswers", "Error", "FillInTheBlanks requires accepted answers in grading definition (via blankAnswers, correctAnswers, or blanks)."));
            return;
        }

        foreach (var bId in blankIds)
        {
            if (!answersMap.TryGetValue(bId, out var accepted) || accepted.Count == 0 || !accepted.Any(a => !string.IsNullOrWhiteSpace(a)))
            {
                issues.Add(new ValidationIssueDto("grading.blankAnswers", "Error", $"Blank '{bId}' must have at least one accepted answer."));
            }
        }
    }

    private static Dictionary<string, List<string>>? ExtractFillInTheBlanksAnswers(JsonObject gradingObj)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (gradingObj["blankAnswers"] is JsonObject blankAnswers)
        {
            foreach (var (bId, bDef) in blankAnswers)
            {
                List<string> accepted;
                if (bDef is JsonArray arr)
                {
                    accepted = arr.Select(x => x?.ToString() ?? string.Empty)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                }
                else
                {
                    accepted = bDef?["acceptedAnswers"]?.AsArray()
                        .Select(x => x?.ToString() ?? string.Empty)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList() ?? new List<string>();
                }
                map[bId] = accepted;
            }
            return map;
        }

        if (gradingObj["correctAnswers"] is JsonArray correctAnswersArray)
        {
            foreach (var item in correctAnswersArray)
            {
                string bId = item?["blankId"]?.ToString() ?? item?["id"]?.ToString() ?? string.Empty;
                var accepted = item?["acceptedAnswers"]?.AsArray()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList() ?? new List<string>();
                if (!string.IsNullOrEmpty(bId))
                {
                    map[bId] = accepted;
                }
            }
            return map;
        }

        if (gradingObj["blanks"] is JsonArray blanksArray)
        {
            foreach (var item in blanksArray)
            {
                string bId = item?["id"]?.ToString() ?? item?["blankId"]?.ToString() ?? string.Empty;
                var accepted = (item?["correctAnswers"]?.AsArray() ?? item?["acceptedAnswers"]?.AsArray())?
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList() ?? new List<string>();
                if (!string.IsNullOrEmpty(bId))
                {
                    map[bId] = accepted;
                }
            }
            return map;
        }

        return null;
    }

    private static void ValidateSentenceUnscramble(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        var tokens = contentObj["tokens"] as JsonArray;
        if (tokens == null || tokens.Count < 2)
        {
            issues.Add(new ValidationIssueDto("content.tokens", "Error", "SentenceUnscramble requires at least 2 tokens."));
            return;
        }

        var tokenIds = tokens
            .Select(t => t?["id"]?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var tokenSet = tokenIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tokenIds.Count != tokenSet.Count)
        {
            issues.Add(new ValidationIssueDto("content.tokens", "Error", "SentenceUnscramble tokens must have unique IDs."));
            return;
        }

        if (gradingObj == null) return;

        var validSeqs = gradingObj["validSequences"] as JsonArray;
        if (validSeqs == null || validSeqs.Count == 0)
        {
            issues.Add(new ValidationIssueDto("grading.validSequences", "Error", "SentenceUnscramble requires non-empty validSequences array."));
            return;
        }

        foreach (var seqNode in validSeqs)
        {
            if (seqNode is not JsonArray seqArr || seqArr.Count == 0)
            {
                issues.Add(new ValidationIssueDto("grading.validSequences", "Error", "Each sequence in validSequences must be a non-empty array of token IDs."));
                continue;
            }

            var seqTokenIds = seqArr.Select(x => x?.ToString() ?? string.Empty).ToList();
            if (seqTokenIds.Count != tokenIds.Count ||
                !tokenSet.SetEquals(seqTokenIds) ||
                seqTokenIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != seqTokenIds.Count)
            {
                issues.Add(new ValidationIssueDto("grading.validSequences", "Error", "Each sequence in validSequences must use all token IDs exactly once."));
            }
        }
    }

    private static void ValidateErrorCorrection(JsonObject contentObj, JsonObject? gradingObj, List<ValidationIssueDto> issues)
    {
        string incorrect = contentObj["incorrectSentence"]?.ToString() ?? contentObj["sentence"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(incorrect))
        {
            issues.Add(new ValidationIssueDto("content.incorrectSentence", "Error", "ErrorCorrection requires non-empty incorrectSentence (or sentence)."));
        }

        if (gradingObj == null) return;

        var refCorrections = (gradingObj["referenceCorrections"] as JsonArray)
            ?? (gradingObj["acceptedCorrections"] as JsonArray);

        if (refCorrections == null || refCorrections.Count == 0 ||
            !refCorrections.Any(x => !string.IsNullOrWhiteSpace(x?.ToString())))
        {
            issues.Add(new ValidationIssueDto("grading.referenceCorrections", "Error", "ErrorCorrection requires at least one non-empty reference correction."));
            return;
        }

        if (!string.IsNullOrWhiteSpace(incorrect))
        {
            string normIncorrect = NormalizeForComparison(incorrect);
            foreach (var corrNode in refCorrections)
            {
                string corrStr = corrNode?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(corrStr) && string.Equals(NormalizeForComparison(corrStr), normIncorrect, StringComparison.Ordinal))
                {
                    issues.Add(new ValidationIssueDto("grading.referenceCorrections", "Error", "Correction sentence cannot be identical to the incorrect sentence."));
                }
            }
        }
    }

    private static void ValidateTranslation(JsonObject contentObj, JsonObject? gradingObj, GradingMode gradingMode, List<ValidationIssueDto> issues)
    {
        if (gradingMode != GradingMode.AiRubric)
        {
            issues.Add(new ValidationIssueDto("gradingMode", "Error", "Translation exercises require GradingMode 'AiRubric'."));
        }

        string rubricCodeTrans = gradingObj?["rubricCode"]?.ToString() ?? string.Empty;
        if (!string.Equals(rubricCodeTrans, "LESSON_TRANSLATION", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssueDto("grading.rubricCode", "Error", "Translation exercises require rubricCode 'LESSON_TRANSLATION'."));
        }

        string sourceText = contentObj["sourceText"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            issues.Add(new ValidationIssueDto("content.sourceText", "Error", "Translation requires non-empty content.sourceText."));
        }

        var refTranslations = gradingObj?["referenceTranslations"] as JsonArray;
        if (refTranslations == null || refTranslations.Count == 0 ||
            !refTranslations.Any(x => !string.IsNullOrWhiteSpace(x?.ToString())))
        {
            issues.Add(new ValidationIssueDto("grading.referenceTranslations", "Error", "Translation requires non-empty grading.referenceTranslations array."));
        }
    }

    private static void ValidateWritingPractice(JsonObject contentObj, JsonObject? gradingObj, GradingMode gradingMode, List<ValidationIssueDto> issues)
    {
        if (gradingMode != GradingMode.AiRubric)
        {
            issues.Add(new ValidationIssueDto("gradingMode", "Error", "WritingPractice exercises require GradingMode 'AiRubric'."));
        }

        string rubricCodeWriting = gradingObj?["rubricCode"]?.ToString() ?? string.Empty;
        if (!string.Equals(rubricCodeWriting, "LESSON_WRITING", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssueDto("grading.rubricCode", "Error", "WritingPractice exercises require rubricCode 'LESSON_WRITING'."));
        }

        string prompt = contentObj["prompt"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            issues.Add(new ValidationIssueDto("content.prompt", "Error", "WritingPractice requires non-empty content.prompt."));
        }
    }

    private static void ValidateReadingComprehension(JsonObject contentObj, GradingMode gradingMode, List<ValidationIssueDto> issues)
    {
        if (gradingMode != GradingMode.Composite)
        {
            issues.Add(new ValidationIssueDto("gradingMode", "Error", "ReadingComprehension exercises require GradingMode 'Composite'."));
        }

        string passage = contentObj["passage"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(passage))
        {
            issues.Add(new ValidationIssueDto("content.passage", "Error", "ReadingComprehension requires non-empty content.passage."));
        }

        if (contentObj["subQuestions"] is not JsonArray subQuestions || subQuestions.Count == 0)
        {
            issues.Add(new ValidationIssueDto("content.subQuestions", "Error", "ReadingComprehension requires a non-empty subQuestions array."));
            return;
        }

        var seenQIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var qNode in subQuestions)
        {
            if (qNode is not JsonObject qObj)
            {
                issues.Add(new ValidationIssueDto("content.subQuestions", "Error", "SubQuestion must be a JSON object."));
                continue;
            }

            string qId = qObj["id"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(qId))
            {
                issues.Add(new ValidationIssueDto("content.subQuestions.id", "Error", "SubQuestion requires a non-empty id."));
            }
            else if (!seenQIds.Add(qId))
            {
                issues.Add(new ValidationIssueDto("content.subQuestions.id", "Error", $"Duplicate subQuestion id '{qId}'."));
            }

            string kind = qObj["questionKind"]?.ToString() ?? qObj["kind"]?.ToString() ?? string.Empty;
            if (string.Equals(kind, "ShortAnswer", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].questionKind", "Error", "ShortAnswer is not supported in ReadingComprehension."));
            }
            else if (kind is not ("SingleChoice" or "MultipleChoice" or "TrueFalse" or "FillBlank"))
            {
                issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].questionKind", "Error", $"Invalid questionKind '{kind}'. Allowed kinds: SingleChoice, MultipleChoice, TrueFalse, FillBlank."));
            }

            if (!qObj.TryGetPropertyValue("maxPoints", out var ptsNode) ||
                ptsNode is not JsonValue ptsVal ||
                !ptsVal.TryGetValue<decimal>(out var pts) ||
                pts <= 0)
            {
                issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].maxPoints", "Error", $"SubQuestion '{qId}' maxPoints must be greater than 0."));
            }

            var qGrading = qObj["grading"] as JsonObject;
            if (qGrading == null)
            {
                issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading", "Error", $"SubQuestion '{qId}' requires a grading definition."));
                continue;
            }

            switch (kind)
            {
                case "SingleChoice":
                case "MultipleChoice":
                    var options = qObj["options"] as JsonArray;
                    if (options == null || options.Count < 2)
                    {
                        issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].options", "Error", $"SubQuestion '{qId}' requires at least 2 options."));
                    }

                    var optIds = options?
                        .Select(o => o?["id"]?.ToString() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var correctOptionIds = qGrading["correctOptionIds"] as JsonArray;
                    if (correctOptionIds == null || correctOptionIds.Count == 0)
                    {
                        issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading.correctOptionIds", "Error", $"SubQuestion '{qId}' requires non-empty correctOptionIds."));
                    }
                    else
                    {
                        if (kind == "SingleChoice" && correctOptionIds.Count != 1)
                        {
                            issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading.correctOptionIds", "Error", $"SingleChoice subQuestion '{qId}' must have exactly 1 correctOptionId."));
                        }

                        foreach (var cNode in correctOptionIds)
                        {
                            string cId = cNode?.ToString() ?? string.Empty;
                            if (!optIds.Contains(cId))
                            {
                                issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading.correctOptionIds", "Error", $"SubQuestion '{qId}' correctOptionId '{cId}' does not match any option."));
                            }
                        }
                    }
                    break;

                case "TrueFalse":
                    if (qGrading["statementAnswer"] is not JsonValue tfVal || !tfVal.TryGetValue<bool>(out _))
                    {
                        issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading.statementAnswer", "Error", $"TrueFalse subQuestion '{qId}' requires a boolean 'statementAnswer' in grading."));
                    }
                    break;

                case "FillBlank":
                    var accepted = qGrading["acceptedAnswers"] as JsonArray;
                    if (accepted == null || accepted.Count == 0 || !accepted.Any(a => !string.IsNullOrWhiteSpace(a?.ToString())))
                    {
                        issues.Add(new ValidationIssueDto($"content.subQuestions[{qId}].grading.acceptedAnswers", "Error", $"FillBlank subQuestion '{qId}' requires non-empty grading.acceptedAnswers."));
                    }
                    break;
            }
        }
    }

    private static string NormalizeForComparison(string s)
    {
        return WhitespaceNormalizeRegex.Replace(s.Trim().ToLowerInvariant(), " ");
    }
}
