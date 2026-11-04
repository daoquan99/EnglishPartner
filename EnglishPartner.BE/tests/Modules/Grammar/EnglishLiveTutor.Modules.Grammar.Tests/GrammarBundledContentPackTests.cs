using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Infrastructure.Grammar;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarBundledContentPackTests
{
    // Core exercise table E01..E10 shared by every authored level (see docs/tasks/20261102-grammar-a2-content).
    private static readonly (ExerciseType Type, int Difficulty)[] CoreTypeTable =
    [
        (ExerciseType.MultipleChoice, 1),
        (ExerciseType.MultipleChoice, 1),
        (ExerciseType.MultipleChoice, 2),
        (ExerciseType.TrueFalse, 1),
        (ExerciseType.Matching, 2),
        (ExerciseType.FillInTheBlanks, 2),
        (ExerciseType.FillInTheBlanks, 3),
        (ExerciseType.SentenceUnscramble, 2),
        (ExerciseType.SentenceUnscramble, 3),
        (ExerciseType.ErrorCorrection, 3),
    ];

    [Fact]
    public void EmbeddedSource_ShouldExposeAllSixLevelPacks()
    {
        var docs = new EmbeddedGrammarContentPackSource().LoadAll();

        Assert.Equal(6, docs.Count);
    }

    [Fact]
    public async Task ImportBundledPacks_ShouldCreateAllHundredStepsWithoutErrors()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var importer = new GrammarContentPackImporter(db, new UnitOfWork(db), new EmbeddedGrammarContentPackSource());

        var result = await importer.ImportAsync();

        Assert.Empty(result.Errors);
        Assert.Equal(100, result.StepsCreated);
        var counts = await db.GrammarSteps.GroupBy(s => s.CefrLevel)
            .Select(g => new { Level = g.Key, Count = g.Count(), Reviews = g.Count(s => s.IsLevelReview) })
            .ToListAsync();
        var expected = new Dictionary<string, int> { ["A1"] = 20, ["A2"] = 20, ["B1"] = 18, ["B2"] = 16, ["C1"] = 14, ["C2"] = 12 };
        foreach (var (level, count) in expected)
        {
            var row = Assert.Single(counts, c => c.Level == level);
            Assert.Equal(count, row.Count);
            Assert.Equal(1, row.Reviews);
        }

        // A1, A2 and B1 ship with 10 deterministic core exercises per step, including difficulty 3 (pass rule).
        Assert.Equal(580, result.ExercisesCreated);
        foreach (var (level, stepCount) in new[] { ("A1", 20), ("A2", 20), ("B1", 18) })
        {
            var levelSteps = await db.GrammarSteps.Where(s => s.CefrLevel == level).Select(s => s.Id).ToListAsync();
            var levelCore = await db.GrammarExercises.Where(e => levelSteps.Contains(e.StepId) && e.IsCore).ToListAsync();
            Assert.Equal(stepCount, levelSteps.Count);
            Assert.Equal(levelCore.Count, levelCore.Select(e => e.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (var stepId in levelSteps)
            {
                var core = levelCore.Where(e => e.StepId == stepId).OrderBy(e => e.Key, StringComparer.Ordinal).ToList();
                Assert.Equal(10, core.Count);
                Assert.All(core, e => Assert.Equal(GradingMode.Deterministic, e.GradingMode));
                Assert.All(core, e => Assert.Equal(10m, e.MaxPoints));
                Assert.Equal(CoreTypeTable, core.Select(e => (e.Type, e.Difficulty)).ToArray());
            }
        }

        var second = await importer.ImportAsync();
        Assert.Equal(0, second.StepsCreated);
        Assert.Equal(0, second.ExercisesCreated);
        Assert.Empty(second.Errors);
        Assert.Equal(100, await db.GrammarSteps.CountAsync());
        Assert.Equal(580, await db.GrammarExercises.CountAsync());
    }
}
