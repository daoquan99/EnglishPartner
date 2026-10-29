using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Infrastructure.Grammar;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarBundledContentPackTests
{
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

        // A1 ships with 10 deterministic core exercises per step, including difficulty 3 (pass rule).
        Assert.Equal(200, result.ExercisesCreated);
        var a1Steps = await db.GrammarSteps.Where(s => s.CefrLevel == "A1").Select(s => s.Id).ToListAsync();
        var a1Core = await db.GrammarExercises.Where(e => a1Steps.Contains(e.StepId) && e.IsCore).ToListAsync();
        foreach (var stepId in a1Steps)
        {
            var core = a1Core.Where(e => e.StepId == stepId).ToList();
            Assert.Equal(10, core.Count);
            Assert.All(core, e => Assert.Equal(GradingMode.Deterministic, e.GradingMode));
            Assert.Contains(core, e => e.Difficulty == 3);
        }

        var second = await importer.ImportAsync();
        Assert.Equal(0, second.StepsCreated);
        Assert.Equal(0, second.ExercisesCreated);
        Assert.Empty(second.Errors);
        Assert.Equal(100, await db.GrammarSteps.CountAsync());
        Assert.Equal(200, await db.GrammarExercises.CountAsync());
    }
}
