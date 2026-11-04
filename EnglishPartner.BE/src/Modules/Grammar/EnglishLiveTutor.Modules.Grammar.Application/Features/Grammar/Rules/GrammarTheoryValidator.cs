using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Rules;

public static class GrammarTheoryValidator
{
    private const int MaxTheoryLength = 60000;
    private const int MaxStringLength = 1000;

    public static List<ValidationIssueDto> Validate(string theoryJson, bool isLevelReview = false)
    {
        var issues = new List<ValidationIssueDto>();

        if (string.IsNullOrWhiteSpace(theoryJson))
        {
            issues.Add(new ValidationIssueDto("$", "Error", "TheoryJson cannot be null or empty."));
            return issues;
        }

        if (theoryJson.Length > MaxTheoryLength)
        {
            issues.Add(new ValidationIssueDto("$", "Error", $"TheoryJson length {theoryJson.Length} exceeds maximum allowed of {MaxTheoryLength} characters."));
        }

        JsonObject rootObj;
        try
        {
            var node = JsonNode.Parse(theoryJson);
            if (node is not JsonObject obj)
            {
                issues.Add(new ValidationIssueDto("$", "Error", "TheoryJson root must be a JSON object."));
                return issues;
            }
            rootObj = obj;
        }
        catch (JsonException ex)
        {
            issues.Add(new ValidationIssueDto("$", "Error", $"Malformed TheoryJson: {ex.Message}"));
            return issues;
        }

        // 1. Objectives (1 - 6 items)
        if (rootObj["objectives"] is not JsonArray objectives || objectives.Count == 0)
        {
            issues.Add(new ValidationIssueDto("$.objectives", "Error", "Theory requires an 'objectives' array with 1 to 6 items."));
        }
        else
        {
            if (objectives.Count > 6)
            {
                issues.Add(new ValidationIssueDto("$.objectives", "Error", $"Objectives count ({objectives.Count}) exceeds maximum of 6."));
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                ValidateString(objectives[i]?.ToString(), $"$.objectives[{i}]", issues, required: true);
            }
        }

        // 2. Sections (1 - 8 items)
        if (rootObj["sections"] is not JsonArray sections || sections.Count == 0)
        {
            issues.Add(new ValidationIssueDto("$.sections", "Error", "Theory requires a 'sections' array with 1 to 8 items."));
        }
        else
        {
            if (sections.Count > 8)
            {
                issues.Add(new ValidationIssueDto("$.sections", "Error", $"Sections count ({sections.Count}) exceeds maximum of 8."));
            }

            for (int sIdx = 0; sIdx < sections.Count; sIdx++)
            {
                string sPath = $"$.sections[{sIdx}]";
                if (sections[sIdx] is not JsonObject sec)
                {
                    issues.Add(new ValidationIssueDto(sPath, "Error", $"Section at index {sIdx} must be a JSON object."));
                    continue;
                }

                // Heading & explanationVi
                ValidateString(sec["heading"]?.ToString(), $"{sPath}.heading", issues, required: true);
                ValidateString(sec["explanationVi"]?.ToString(), $"{sPath}.explanationVi", issues, required: true);

                // Formulas (optional, max 6)
                if (sec["formulas"] is JsonArray formulas)
                {
                    if (formulas.Count > 6)
                    {
                        issues.Add(new ValidationIssueDto($"{sPath}.formulas", "Error", $"Formulas count ({formulas.Count}) exceeds maximum of 6."));
                    }

                    for (int fIdx = 0; fIdx < formulas.Count; fIdx++)
                    {
                        string fPath = $"{sPath}.formulas[{fIdx}]";
                        if (formulas[fIdx] is JsonObject formulaObj)
                        {
                            ValidateString(formulaObj["label"]?.ToString(), $"{fPath}.label", issues, required: false);
                            ValidateString(formulaObj["pattern"]?.ToString(), $"{fPath}.pattern", issues, required: true);
                        }
                        else
                        {
                            ValidateString(formulas[fIdx]?.ToString(), fPath, issues, required: true);
                        }
                    }
                }

                // Examples (optional, max 8, en + vi required)
                if (sec["examples"] is JsonArray examples)
                {
                    if (examples.Count > 8)
                    {
                        issues.Add(new ValidationIssueDto($"{sPath}.examples", "Error", $"Examples count ({examples.Count}) exceeds maximum of 8."));
                    }

                    for (int eIdx = 0; eIdx < examples.Count; eIdx++)
                    {
                        string ePath = $"{sPath}.examples[{eIdx}]";
                        if (examples[eIdx] is not JsonObject exObj)
                        {
                            issues.Add(new ValidationIssueDto(ePath, "Error", "Example must be a JSON object containing 'en' and 'vi'."));
                            continue;
                        }

                        ValidateString(exObj["en"]?.ToString(), $"{ePath}.en", issues, required: true);
                        ValidateString(exObj["vi"]?.ToString(), $"{ePath}.vi", issues, required: true);
                        ValidateString(exObj["highlight"]?.ToString(), $"{ePath}.highlight", issues, required: false);
                    }
                }

                // Notes (optional)
                if (sec["notes"] is JsonArray notes)
                {
                    for (int nIdx = 0; nIdx < notes.Count; nIdx++)
                    {
                        ValidateString(notes[nIdx]?.ToString(), $"{sPath}.notes[{nIdx}]", issues, required: false);
                    }
                }
            }
        }

        // 3. Common mistakes (0..8, required 1..8 for non-review steps)
        if (rootObj["commonMistakes"] is not JsonArray mistakes)
        {
            if (!isLevelReview)
            {
                issues.Add(new ValidationIssueDto("$.commonMistakes", "Error", "Theory requires a non-empty 'commonMistakes' array for standard steps."));
            }
        }
        else
        {
            if (!isLevelReview && mistakes.Count == 0)
            {
                issues.Add(new ValidationIssueDto("$.commonMistakes", "Error", "Standard grammar steps require at least 1 item in 'commonMistakes'."));
            }

            if (mistakes.Count > 8)
            {
                issues.Add(new ValidationIssueDto("$.commonMistakes", "Error", $"Common mistakes count ({mistakes.Count}) exceeds maximum of 8."));
            }

            for (int mIdx = 0; mIdx < mistakes.Count; mIdx++)
            {
                string mPath = $"$.commonMistakes[{mIdx}]";
                if (mistakes[mIdx] is not JsonObject mObj)
                {
                    issues.Add(new ValidationIssueDto(mPath, "Error", "Common mistake item must be a JSON object containing 'wrong', 'right', and 'explanationVi'."));
                    continue;
                }

                ValidateString(mObj["wrong"]?.ToString(), $"{mPath}.wrong", issues, required: true);
                ValidateString(mObj["right"]?.ToString(), $"{mPath}.right", issues, required: true);
                ValidateString(mObj["explanationVi"]?.ToString(), $"{mPath}.explanationVi", issues, required: true);
            }
        }

        // 4. Tips (optional, max 8)
        if (rootObj["tips"] is JsonArray tips)
        {
            if (tips.Count > 8)
            {
                issues.Add(new ValidationIssueDto("$.tips", "Error", $"Tips count ({tips.Count}) exceeds maximum of 8."));
            }

            for (int tIdx = 0; tIdx < tips.Count; tIdx++)
            {
                ValidateString(tips[tIdx]?.ToString(), $"$.tips[{tIdx}]", issues, required: false);
            }
        }

        // 5. Summary (1 - 6 items)
        if (rootObj["summary"] is not JsonArray summary || summary.Count == 0)
        {
            issues.Add(new ValidationIssueDto("$.summary", "Error", "Theory requires a 'summary' array with 1 to 6 items."));
        }
        else
        {
            if (summary.Count > 6)
            {
                issues.Add(new ValidationIssueDto("$.summary", "Error", $"Summary count ({summary.Count}) exceeds maximum of 6."));
            }

            for (int suIdx = 0; suIdx < summary.Count; suIdx++)
            {
                ValidateString(summary[suIdx]?.ToString(), $"$.summary[{suIdx}]", issues, required: true);
            }
        }

        return issues;
    }

    private static void ValidateString(string? value, string path, List<ValidationIssueDto> issues, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                issues.Add(new ValidationIssueDto(path, "Error", $"Field at '{path}' is required and cannot be empty."));
            }
            return;
        }

        if (value.Length > MaxStringLength)
        {
            issues.Add(new ValidationIssueDto(path, "Error", $"String at '{path}' exceeds {MaxStringLength} characters ({value.Length})."));
        }
    }
}
