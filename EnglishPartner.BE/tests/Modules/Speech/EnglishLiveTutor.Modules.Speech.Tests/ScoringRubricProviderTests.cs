using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class ScoringRubricProviderTests
{
    [Fact]
    public async Task GetByCodeAndVersionAsync_MapsAllCriteriaInEntityOrder()
    {
        var options = CreateInMemoryOptions();
        var rubric = CreateRubric();
        await using (var seedDb = new AppDbContext(options))
        {
            seedDb.ScoringRubrics.Add(rubric);
            await seedDb.SaveChangesAsync();
        }

        var expectedCriteria = rubric.Criteria.ToList();
        await using var db = new AppDbContext(options);
        var provider = new ScoringRubricProvider(db);

        var byCode = await provider.GetByCodeAndVersionAsync(rubric.Code, rubric.Version);

        AssertSnapshotMatchesEntity(byCode, rubric, expectedCriteria);
    }

    [Fact]
    public async Task LookupMethods_ReturnNullOrEmptyWhenRubricDoesNotExist()
    {
        await using var db = new AppDbContext(CreateInMemoryOptions());
        var provider = new ScoringRubricProvider(db);

        Assert.Null(await provider.GetByCodeAndVersionAsync("MISSING", 1));
        Assert.Empty(await provider.ListExistingCodesAsync(["MISSING"]));
    }

    [Fact]
    public async Task ListExistingCodesAsync_UsesOnlyNonDeletedMatchingCodes()
    {
        var options = CreateInMemoryOptions();
        var active = CreateRubric();
        var deleted = new ScoringRubricEntity("LESSON_DELETED", "Deleted", version: 1);
        deleted.SoftDelete(DateTimeOffset.UtcNow, "test");
        await using (var seedDb = new AppDbContext(options))
        {
            seedDb.ScoringRubrics.AddRange(active, deleted);
            await seedDb.SaveChangesAsync();
        }

        await using var db = new AppDbContext(options);
        var codes = await new ScoringRubricProvider(db)
            .ListExistingCodesAsync([active.Code, deleted.Code, "MISSING"]);

        Assert.Equal([active.Code], codes);
    }

    [Fact]
    public async Task LessonAiGrader_SnapshotFromEntity_PreservesPromptAndWeightedResult()
    {
        var rubric = CreateRubric();
        var snapshot = new ScoringRubricSnapshot(
            rubric.Code,
            rubric.Version,
            rubric.Criteria.Select(c => new ScoringRubricCriterionSnapshot(
                c.Code,
                c.Name,
                c.Description,
                c.Weight,
                c.DisplayOrder,
                c.IsEnabled,
                c.DescriptorLevel0,
                c.DescriptorLevel1,
                c.DescriptorLevel2,
                c.DescriptorLevel3,
                c.DescriptorLevel4)).ToList());

        var runner = new CapturingAgyRunner(
            """
            {
              "isAssessable": true,
              "criteria": [
                { "code": "MEANING", "rawScore": 4, "feedback": "Meaning is accurate." },
                { "code": "GRAMMAR", "rawScore": 3, "feedback": "Grammar is good." }
              ],
              "strengths": ["Clear meaning"],
              "improvements": ["Polish grammar"],
              "suggestedAnswer": "He goes to school every day."
            }
            """);
        var grader = new LessonAiGrader(
            new HttpClient(),
            new FakeAudioRecordingStore(),
            new FakeLessonMediaStorage(),
            runner,
            Options.Create(new LessonAssessmentOptions()),
            NullLogger<LessonAiGrader>.Instance);

        var result = await grader.GradeSubmissionAsync(
            ExerciseType.Translation,
            "Translate the sentence.",
            "B1",
            snapshot,
            JsonSerializer.Serialize(new { writtenText = "Anh ấy đi học mỗi ngày." }),
            maxPoints: 5m,
            aiConfig: new LessonAiExecutionConfigDto("unused", "test-agy-model", "agy"));

        Assert.Contains("Rubric (LESSON_TRANSLATION v1):", runner.LastPrompt);
        Assert.Contains(
            "- Code: 'GRAMMAR', Name: 'Grammar', Weight: 40%: Description for grammar.. Level 0..4 scale.",
            runner.LastPrompt);
        Assert.Contains(
            "- Code: 'MEANING', Name: 'Meaning', Weight: 60%: Description for meaning.. Level 0..4 scale.",
            runner.LastPrompt);
        Assert.Equal(90m, result.Score100);
        Assert.Equal(4.5m, result.PointsEarned);
        Assert.Equal("Level 4 descriptor for MEANING.", result.CriteriaBreakdown[1].Descriptor);
        Assert.Equal("Level 3 descriptor for GRAMMAR.", result.CriteriaBreakdown[0].Descriptor);
    }

    [Fact]
    public async Task LessonAiGrader_DisabledAgy_RejectsRequestWithoutCallingGemini()
    {
        var rubric = CreateRubric();
        var snapshot = CreateSnapshot(rubric);
        var handler = new ThrowingHttpMessageHandler();
        var runner = new AgyCliRunner(
            Options.Create(new AntigravityCliOptions
            {
                Enabled = false,
                AllowedModels = ["gemini-3.8-flash-high"]
            }),
            NullLogger<AgyCliRunner>.Instance);
        var grader = new LessonAiGrader(
            new HttpClient(handler),
            new FakeAudioRecordingStore(),
            new FakeLessonMediaStorage(),
            runner,
            Options.Create(new LessonAssessmentOptions()),
            NullLogger<LessonAiGrader>.Instance);

        var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
            grader.GradeSubmissionAsync(
                ExerciseType.Translation,
                "Translate the sentence.",
                "B1",
                snapshot,
                JsonSerializer.Serialize(new { writtenText = "Anh ấy đi học mỗi ngày." }),
                maxPoints: 5m,
                aiConfig: new LessonAiExecutionConfigDto(null, "gemini-3.8-flash-high", "agy")));

        Assert.Equal("agy_local_disabled", error.Message);
        Assert.Equal(0, handler.CallCount);
    }

    private static ScoringRubricSnapshot CreateSnapshot(ScoringRubricEntity rubric) =>
        new(
            rubric.Code,
            rubric.Version,
            rubric.Criteria.Select(c => new ScoringRubricCriterionSnapshot(
                c.Code,
                c.Name,
                c.Description,
                c.Weight,
                c.DisplayOrder,
                c.IsEnabled,
                c.DescriptorLevel0,
                c.DescriptorLevel1,
                c.DescriptorLevel2,
                c.DescriptorLevel3,
                c.DescriptorLevel4)).ToList());

    private static ScoringRubricEntity CreateRubric()
    {
        var rubric = new ScoringRubricEntity(
            "LESSON_TRANSLATION",
            "Translation Rubric",
            version: 1,
            assessmentType: "LessonExercise",
            assessmentScope: "LessonExercise");
        rubric.AddCriterion(
            "MEANING",
            "Meaning",
            "Description for meaning.",
            60m,
            displayOrder: 2,
            descriptorLevel3: "Level 3 descriptor for MEANING.",
            descriptorLevel4: "Level 4 descriptor for MEANING.");
        rubric.AddCriterion(
            "GRAMMAR",
            "Grammar",
            "Description for grammar.",
            40m,
            displayOrder: 1,
            descriptorLevel3: "Level 3 descriptor for GRAMMAR.",
            descriptorLevel4: "Level 4 descriptor for GRAMMAR.");
        return rubric;
    }

    private static void AssertSnapshotMatchesEntity(
        ScoringRubricSnapshot? snapshot,
        ScoringRubricEntity rubric,
        IReadOnlyList<EnglishLiveTutor.Domain.Pronunciation.Entities.ScoringRubricCriterionEntity> expectedCriteria)
    {
        Assert.NotNull(snapshot);
        Assert.Equal(rubric.Code, snapshot.Code);
        Assert.Equal(rubric.Version, snapshot.Version);
        Assert.Equal(expectedCriteria.Select(c => c.Code), snapshot.Criteria.Select(c => c.Code));

        for (int i = 0; i < expectedCriteria.Count; i++)
        {
            var expected = expectedCriteria[i];
            var actual = snapshot.Criteria[i];
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Description, actual.Description);
            Assert.Equal(expected.Weight, actual.Weight);
            Assert.Equal(expected.DisplayOrder, actual.DisplayOrder);
            Assert.Equal(expected.IsEnabled, actual.IsEnabled);
            Assert.Equal(expected.DescriptorLevel0, actual.DescriptorLevel0);
            Assert.Equal(expected.DescriptorLevel1, actual.DescriptorLevel1);
            Assert.Equal(expected.DescriptorLevel2, actual.DescriptorLevel2);
            Assert.Equal(expected.DescriptorLevel3, actual.DescriptorLevel3);
            Assert.Equal(expected.DescriptorLevel4, actual.DescriptorLevel4);
        }
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    private sealed class CapturingAgyRunner : IAgyCliRunner
    {
        private readonly string _response;

        public CapturingAgyRunner(string response)
        {
            _response = response;
        }

        public string LastPrompt { get; private set; } = string.Empty;
        public bool IsEnabled => true;
        public IReadOnlyCollection<string> AllowedModels => ["test-agy-model"];

        public Task<string> ExecutePromptAsync(
            string prompt,
            string? modelCode = null,
            CancellationToken ct = default)
        {
            LastPrompt = prompt;
            return Task.FromResult(_response);
        }

        public bool IsAgyModel(string? modelCode) => true;

        public string NormalizeModel(string? modelCode) => "test-agy-model";
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Gemini must not be called when AGY is disabled.");
        }
    }
}
