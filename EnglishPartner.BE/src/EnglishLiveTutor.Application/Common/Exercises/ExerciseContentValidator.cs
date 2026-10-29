using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Common.Exercises;

public static class ExerciseContentValidator
{
    public static List<ValidationIssueDto> ValidateExerciseTypeSemantics(
        string path,
        ExerciseType type,
        JsonObject content,
        JsonObject? grading,
        Dictionary<string, List<string>>? rubricCodesToVerify = null)
    {
        var issues = new List<ValidationIssueDto>();

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

                    if (rubricCodesToVerify != null)
                    {
                        if (!rubricCodesToVerify.TryGetValue(rubricCode, out var paths))
                        {
                            paths = new List<string>();
                            rubricCodesToVerify[rubricCode] = paths;
                        }
                        paths.Add($"{path}.grading.rubricCode");
                    }
                }
                break;

            default:
                break;
        }

        return issues;
    }
}
