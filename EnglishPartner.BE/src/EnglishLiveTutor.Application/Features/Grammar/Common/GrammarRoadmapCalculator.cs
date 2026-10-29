using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Common;

public static class GrammarRoadmapCalculator
{
    public static readonly string[] LevelOrder = GrammarStepEntity.Levels;

    public static GrammarRoadmapDto ComputeRoadmap(
        IReadOnlyList<GrammarStepEntity> allPublishedSteps,
        IReadOnlyList<UserGrammarStepProgressEntity> userProgresses)
    {
        var progressMap = userProgresses.ToDictionary(p => p.StepId);
        var levelDtos = new List<GrammarLevelDto>();

        bool previousLevelPassed = true; // A1 always has previous level passed

        foreach (var level in LevelOrder)
        {
            var levelSteps = allPublishedSteps
                .Where(s => string.Equals(s.CefrLevel, level, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Order)
                .ToList();

            bool canTakeLevelTest = previousLevelPassed;
            var stepDtos = new List<GrammarStepRoadmapItemDto>();
            bool prevStepPassed = true;

            for (int i = 0; i < levelSteps.Count; i++)
            {
                var step = levelSteps[i];
                progressMap.TryGetValue(step.Id, out var prog);

                bool isPassed = prog != null && prog.Status == GrammarStepProgressStatus.Passed;
                bool isUnlockedByRule;

                if (i == 0)
                {
                    if (string.Equals(level, "A1", StringComparison.OrdinalIgnoreCase))
                    {
                        isUnlockedByRule = true; // A1-01 always unlocked
                    }
                    else
                    {
                        isUnlockedByRule = previousLevelPassed;
                    }
                }
                else
                {
                    isUnlockedByRule = prevStepPassed;
                }

                bool isUnlocked = isPassed || isUnlockedByRule || (prog != null && prog.Status == GrammarStepProgressStatus.Unlocked);
                string status = isPassed ? "Passed" : (isUnlocked ? "Unlocked" : "Locked");

                stepDtos.Add(new GrammarStepRoadmapItemDto(
                    Code: step.Code,
                    Order: step.Order,
                    TitleVi: step.TitleVi,
                    TitleEn: step.TitleEn,
                    SummaryVi: step.SummaryVi,
                    IsLevelReview: step.IsLevelReview,
                    Status: status,
                    BestScore100: prog?.BestScore100,
                    PassedBy: prog?.PassedBy?.ToString()
                ));

                prevStepPassed = isPassed;
            }

            bool thisLevelPassed = levelSteps.Count > 0 && stepDtos.All(s => s.Status == "Passed");
            bool isLevelUnlocked = string.Equals(level, "A1", StringComparison.OrdinalIgnoreCase)
                || previousLevelPassed
                || stepDtos.Any(s => s.Status != "Locked");

            levelDtos.Add(new GrammarLevelDto(
                Level: level,
                IsUnlocked: isLevelUnlocked,
                IsPassed: thisLevelPassed,
                CanTakeLevelTest: canTakeLevelTest,
                Steps: stepDtos
            ));

            previousLevelPassed = thisLevelPassed;
        }

        return new GrammarRoadmapDto(levelDtos);
    }

    public static (string Status, decimal? BestScore100, string? PassedBy) GetStepStatus(
        string stepCode,
        IReadOnlyList<GrammarStepEntity> allPublishedSteps,
        IReadOnlyList<UserGrammarStepProgressEntity> userProgresses)
    {
        var roadmap = ComputeRoadmap(allPublishedSteps, userProgresses);
        var item = roadmap.Levels.SelectMany(l => l.Steps).FirstOrDefault(s => string.Equals(s.Code, stepCode, StringComparison.OrdinalIgnoreCase));
        return (item?.Status ?? "Locked", item?.BestScore100, item?.PassedBy);
    }

    public static bool CanTakeLevelTest(
        string cefrLevel,
        IReadOnlyList<GrammarStepEntity> allPublishedSteps,
        IReadOnlyList<UserGrammarStepProgressEntity> userProgresses)
    {
        var roadmap = ComputeRoadmap(allPublishedSteps, userProgresses);
        var level = roadmap.Levels.FirstOrDefault(l => string.Equals(l.Level, cefrLevel, StringComparison.OrdinalIgnoreCase));
        return level?.CanTakeLevelTest ?? false;
    }

    public static async Task<List<GrammarStepEntity>> GetSortedPublishedStepsAsync(IAppDbContext db, CancellationToken ct = default)
    {
        var steps = await db.GrammarSteps
            .AsNoTracking()
            .Where(s => s.IsPublished && !s.IsDeleted)
            .Select(s => GrammarStepEntity.Create(
                s.Code,
                s.CefrLevel,
                s.Order,
                s.TitleVi,
                s.TitleEn,
                s.SummaryVi,
                "{}",
                s.TheoryVersion,
                s.IsLevelReview,
                s.IsPublished,
                s.Id))
            .ToListAsync(ct);

        return steps
            .OrderBy(s => Array.IndexOf(LevelOrder, s.CefrLevel.ToUpperInvariant()))
            .ThenBy(s => s.Order)
            .ToList();
    }
}
