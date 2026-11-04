using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Domain.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Services;

public interface IGrammarExercisePromptContextLoader
{
    Task<GrammarExercisePromptInput> LoadContextAsync(
        string stepCode,
        IReadOnlyList<GrammarExercisePromptRequestedItem> requestedItems,
        string? learnerNote = null,
        CancellationToken ct = default);
}

public sealed class GrammarExercisePromptContextLoader : IGrammarExercisePromptContextLoader
{
    private static readonly Dictionary<string, int> CefrRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A1"] = 1,
        ["A2"] = 2,
        ["B1"] = 3,
        ["B2"] = 4,
        ["C1"] = 5,
        ["C2"] = 6
    };

    private readonly IGrammarDbContext _db;

    public GrammarExercisePromptContextLoader(IGrammarDbContext db)
    {
        _db = db;
    }

    public async Task<GrammarExercisePromptInput> LoadContextAsync(
        string stepCode,
        IReadOnlyList<GrammarExercisePromptRequestedItem> requestedItems,
        string? learnerNote = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stepCode))
        {
            throw new NotFoundException("StepCode cannot be empty.");
        }

        string normalizedCode = stepCode.Trim().ToUpperInvariant();
        var currentStep = await _db.GrammarSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (currentStep == null)
        {
            throw new NotFoundException($"Step '{stepCode}' không tồn tại.");
        }

        int currentRank = CefrRank.TryGetValue(currentStep.CefrLevel, out var cr) ? cr : 0;

        // Fetch all published/existing steps to determine prior and later topics
        var allSteps = await _db.GrammarSteps.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.CefrLevel)
            .ThenBy(s => s.Order)
            .ToListAsync(ct);

        // Sort accurately using CefrRank and Order
        var orderedSteps = allSteps
            .OrderBy(s => CefrRank.TryGetValue(s.CefrLevel, out var r) ? r : 99)
            .ThenBy(s => s.Order)
            .ToList();

        int currentIndex = orderedSteps.FindIndex(s => s.Id == currentStep.Id);

        // Prior steps: all steps before currentIndex
        var priorStepEntities = currentIndex > 0
            ? orderedSteps.Take(currentIndex).ToList()
            : new List<GrammarStepEntity>();

        // Later topics to avoid: up to 5 steps after currentIndex
        var laterStepEntities = currentIndex >= 0 && currentIndex + 1 < orderedSteps.Count
            ? orderedSteps.Skip(currentIndex + 1).Take(5).ToList()
            : new List<GrammarStepEntity>();

        // Format prior steps
        var priorStepTitles = FormatPriorStepTitles(priorStepEntities, currentStep.CefrLevel);

        var laterTopicsToAvoid = laterStepEntities
            .Select(s => s.TitleEn)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        List<string> formulas = new();
        List<string> commonMistakes = new();

        if (currentStep.IsLevelReview)
        {
            // For IsLevelReview: substitute formulas/commonMistakes with topics & main formulas of all steps in the level
            var levelSteps = orderedSteps
                .Where(s => string.Equals(s.CefrLevel, currentStep.CefrLevel, StringComparison.OrdinalIgnoreCase) && s.Id != currentStep.Id)
                .ToList();

            foreach (var step in levelSteps)
            {
                var stepFormulas = ExtractFormulas(step.TheoryJson, maxCount: 2);
                if (stepFormulas.Count > 0)
                {
                    formulas.Add($"{step.TitleEn}: {string.Join("; ", stepFormulas)}");
                }
                else
                {
                    formulas.Add($"{step.TitleEn}");
                }
            }

            // Common mistakes from level steps (up to 8)
            foreach (var step in levelSteps)
            {
                var stepMistakes = ExtractCommonMistakes(step.TheoryJson, maxCount: 2);
                commonMistakes.AddRange(stepMistakes);
                if (commonMistakes.Count >= 8) break;
            }
        }
        else
        {
            formulas = ExtractFormulas(currentStep.TheoryJson, maxCount: 30);
            commonMistakes = ExtractCommonMistakes(currentStep.TheoryJson, maxCount: 8);
        }

        return new GrammarExercisePromptInput(
            StepCode: currentStep.Code,
            CefrLevel: currentStep.CefrLevel,
            TitleEn: currentStep.TitleEn,
            TitleVi: currentStep.TitleVi,
            Formulas: formulas,
            CommonMistakes: commonMistakes,
            PriorSteps: priorStepTitles,
            LaterTopicsToAvoid: laterTopicsToAvoid,
            RequestedItems: requestedItems,
            LearnerNote: learnerNote,
            IsLevelReview: currentStep.IsLevelReview
        );
    }

    private static List<string> FormatPriorStepTitles(List<GrammarStepEntity> priorSteps, string currentLevel)
    {
        if (priorSteps.Count <= 40)
        {
            return priorSteps.Select(s => s.TitleEn).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        }

        // Group complete earlier levels: e.g. "All of A1", "All of A2"
        var result = new List<string>();
        var priorGroupedByLevel = priorSteps
            .GroupBy(s => s.CefrLevel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in priorGroupedByLevel)
        {
            if (!string.Equals(group.Key, currentLevel, StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"All of {group.Key.ToUpperInvariant()}");
            }
            else
            {
                result.AddRange(group.Select(s => s.TitleEn));
            }
        }

        return result;
    }

    private static List<string> ExtractFormulas(string? theoryJson, int maxCount)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(theoryJson)) return result;

        try
        {
            var node = JsonNode.Parse(theoryJson);
            if (node?["sections"] is JsonArray sections)
            {
                foreach (var sec in sections)
                {
                    if (sec?["formulas"] is JsonArray formulasArr)
                    {
                        foreach (var fNode in formulasArr)
                        {
                            if (result.Count >= maxCount) break;

                            if (fNode is JsonObject fObj)
                            {
                                string label = fObj["label"]?.ToString() ?? string.Empty;
                                string pattern = fObj["pattern"]?.ToString() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(pattern))
                                {
                                    result.Add(!string.IsNullOrWhiteSpace(label) ? $"{label}: {pattern}" : pattern);
                                }
                            }
                            else if (fNode is not null)
                            {
                                string str = fNode.ToString();
                                if (!string.IsNullOrWhiteSpace(str))
                                {
                                    result.Add(str);
                                }
                            }
                        }
                    }
                    if (result.Count >= maxCount) break;
                }
            }
        }
        catch
        {
            // Ignore parse errors if theoryJson is malformed
        }

        return result;
    }

    private static List<string> ExtractCommonMistakes(string? theoryJson, int maxCount)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(theoryJson)) return result;

        try
        {
            var node = JsonNode.Parse(theoryJson);
            if (node?["commonMistakes"] is JsonArray mistakesArr)
            {
                foreach (var mNode in mistakesArr)
                {
                    if (result.Count >= maxCount) break;

                    if (mNode is JsonObject mObj)
                    {
                        string wrong = mObj["wrong"]?.ToString() ?? string.Empty;
                        string right = mObj["right"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(wrong) && !string.IsNullOrWhiteSpace(right))
                        {
                            result.Add($"{wrong} → {right}");
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return result;
    }
}
