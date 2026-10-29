using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarContentPackImporterTests
{
    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    private sealed class FakePackSource : IGrammarContentPackSource
    {
        private readonly List<GrammarContentPackDocument> _docs;

        public FakePackSource(params GrammarContentPackDocument[] docs)
        {
            _docs = docs.ToList();
        }

        public void SetDocuments(params GrammarContentPackDocument[] docs)
        {
            _docs.Clear();
            _docs.AddRange(docs);
        }

        public IReadOnlyList<GrammarContentPackDocument> LoadAll() => _docs;
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        public int CommitCount { get; private set; }

        public CountingUnitOfWork(IUnitOfWork inner)
        {
            _inner = inner;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            CommitCount++;
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken ct = default)
        {
            return _inner.ExecuteInTransactionAsync(operation, ct);
        }
    }

    private static string SampleTheoryJson => """
    {
      "objectives": ["Mục tiêu 1."],
      "sections": [
        {
          "heading": "Cấu trúc",
          "explanationVi": "Giải thích chi tiết.",
          "examples": [{ "en": "I am here.", "vi": "Tôi ở đây." }]
        }
      ],
      "commonMistakes": [
        { "wrong": "I hungry.", "right": "I am hungry.", "explanationVi": "Thiếu be." }
      ],
      "summary": ["Ghi nhớ quy tắc."]
    }
    """;

    private static string BuildPackJson(int packVersion, int theoryVersion, string? customTheoryHeading = null, bool includeDuplicateEx = false)
    {
        string heading = customTheoryHeading ?? "Cấu trúc";
        string duplicateBlock = includeDuplicateEx
            ? """
            ,
            {
              "key": "A1-01-E02",
              "type": "MultipleChoice",
              "difficulty": 1,
              "title": "Question 2",
              "instructions": "Choose one",
              "maxPoints": 10,
              "gradingMode": "Deterministic",
              "isCore": true,
              "content": {
                "options": [{ "id": "o1", "text": "A" }, { "id": "o2", "text": "B" }],
                "selectionMode": "Single"
              },
              "grading": { "correctOptionIds": ["o1"] }
            }
            """
            : "";

        return $$"""
        {
          "packVersion": {{packVersion}},
          "level": "A1",
          "steps": [
            {
              "code": "A1-01",
              "order": 1,
              "titleVi": "Từ loại cơ bản",
              "titleEn": "Basic Parts of Speech",
              "summaryVi": "Tóm tắt từ loại.",
              "isLevelReview": false,
              "theoryVersion": {{theoryVersion}},
              "theory": {
                "objectives": ["Mục tiêu 1."],
                "sections": [
                  {
                    "heading": "{{heading}}",
                    "explanationVi": "Giải thích chi tiết.",
                    "examples": [{ "en": "I am here.", "vi": "Tôi ở đây." }]
                  }
                ],
                "commonMistakes": [
                  { "wrong": "I hungry.", "right": "I am hungry.", "explanationVi": "Thiếu be." }
                ],
                "summary": ["Ghi nhớ quy tắc."]
              },
              "exercises": [
                {
                  "key": "A1-01-E01",
                  "type": "MultipleChoice",
                  "difficulty": 1,
                  "title": "Question 1",
                  "instructions": "Choose one",
                  "maxPoints": 10,
                  "gradingMode": "Deterministic",
                  "isCore": true,
                  "content": {
                    "options": [{ "id": "o1", "text": "A" }, { "id": "o2", "text": "B" }],
                    "selectionMode": "Single"
                  },
                  "grading": { "correctOptionIds": ["o1"] }
                }
                {{duplicateBlock}}
              ]
            }
          ]
        }
        """;
    }

    [Fact]
    public async Task ImportAsync_CreatesStepAndExercise_AndIsIdempotentOnSecondRun()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var innerUow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(innerUow);

        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1)));
        var importer = new GrammarContentPackImporter(db, countingUow, packSource);

        // Run 1: Create
        var result1 = await importer.ImportAsync();
        Assert.Equal(1, result1.StepsCreated);
        Assert.Equal(0, result1.StepsUpdated);
        Assert.Equal(0, result1.StepsSkipped);
        Assert.Equal(1, result1.ExercisesCreated);
        Assert.Equal(0, result1.ExercisesSkipped);
        Assert.Empty(result1.Errors);
        Assert.Equal(1, countingUow.CommitCount);

        var step = await db.GrammarSteps.FirstOrDefaultAsync(s => s.Code == "A1-01");
        Assert.NotNull(step);
        Assert.Equal("A1-01", step.Code);
        Assert.Equal(1, step.TheoryVersion);

        var ex = await db.GrammarExercises.FirstOrDefaultAsync(e => e.StepId == step.Id);
        Assert.NotNull(ex);
        Assert.Equal("A1-01-E01", ex.Key);

        // Run 2: Exact same pack -> Idempotent
        var result2 = await importer.ImportAsync();
        Assert.Equal(0, result2.StepsCreated);
        Assert.Equal(0, result2.StepsUpdated); // Nothing changed -> StepsUpdated == 0
        Assert.Equal(0, result2.ExercisesCreated);
        Assert.Equal(1, result2.ExercisesSkipped); // skipped duplicate exercise
        Assert.Equal(2, countingUow.CommitCount); // one commit per run
    }

    [Fact]
    public async Task ImportAsync_TheoryEditedByAdmin_IsNotOverwritten()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1)));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        await importer.ImportAsync();

        var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
        step.EditTheoryByAdmin("{\"customAdmin\":\"true\"}");
        await uow.CommitChangesAsync();

        // Admin edited theory version is now 2
        Assert.True(step.TheoryEditedByAdmin);

        // New pack with higher theoryVersion 3
        packSource.SetDocuments(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 3, "New Pack Heading")));
        var result = await importer.ImportAsync();

        Assert.Equal(0, result.StepsCreated);
        Assert.Equal(0, result.StepsUpdated); // Metadata identical, admin rejected theory update -> 0 updated

        var stepAfter = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
        Assert.Equal("{\"customAdmin\":\"true\"}", stepAfter.TheoryJson);
        Assert.True(stepAfter.TheoryEditedByAdmin);
    }

    [Fact]
    public async Task ImportAsync_HigherTheoryVersion_AppliedWhenNotEditedByAdmin()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1)));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        await importer.ImportAsync();

        // Pack upgraded to theory version 2
        packSource.SetDocuments(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 2, "Upgraded Heading")));
        await importer.ImportAsync();

        var stepAfter = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
        Assert.Equal(2, stepAfter.TheoryVersion);
        Assert.Contains("Upgraded Heading", stepAfter.TheoryJson);
    }

    [Fact]
    public async Task ImportAsync_StepWithError_Skipped_ValidStepImported()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        const string packWithMixedSteps = """
        {
          "packVersion": 1,
          "level": "A1",
          "steps": [
            {
              "code": "INVALID-CODE",
              "order": 1,
              "titleVi": "Lỗi",
              "titleEn": "Error",
              "summaryVi": "Tóm tắt",
              "isLevelReview": false,
              "theoryVersion": 1,
              "theory": {}
            },
            {
              "code": "A1-02",
              "order": 2,
              "titleVi": "Đại từ",
              "titleEn": "Pronouns",
              "summaryVi": "Tóm tắt đại từ.",
              "isLevelReview": false,
              "theoryVersion": 1,
              "theory": {
                "objectives": ["Học đại từ."],
                "sections": [{ "heading": "Phân loại", "explanationVi": "Đại từ nhân xưng." }],
                "commonMistakes": [{ "wrong": "Me like.", "right": "I like.", "explanationVi": "Dùng đại từ chủ ngữ." }],
                "summary": ["I, you, he, she..."]
              }
            }
          ]
        }
        """;

        var packSource = new FakePackSource(new GrammarContentPackDocument("mixed.json", packWithMixedSteps));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        var result = await importer.ImportAsync();

        Assert.Equal(1, result.StepsSkipped);
        Assert.Equal(1, result.StepsCreated);
        Assert.NotEmpty(result.Errors);

        var validStep = await db.GrammarSteps.FirstOrDefaultAsync(s => s.Code == "A1-02");
        Assert.NotNull(validStep);
    }

    [Fact]
    public async Task ImportAsync_DuplicateExerciseInStep_Skipped()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        // Pack with 2 exercises having identical content/hash
        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1, includeDuplicateEx: true)));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        var result = await importer.ImportAsync();

        Assert.Equal(1, result.StepsCreated);
        Assert.Equal(1, result.ExercisesCreated);
        Assert.Equal(1, result.ExercisesSkipped); // Second exercise has duplicate content hash
    }

    [Fact]
    public async Task ImportAsync_SoftDeletedStep_Skipped_NoException_NoNewRecord()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1)));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        // Run 1: Create step
        await importer.ImportAsync();
        var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");

        // Soft delete step
        step.SoftDelete(DateTimeOffset.UtcNow, "admin");
        await uow.CommitChangesAsync();

        // Run 2: Import again with same pack
        var result2 = await importer.ImportAsync();

        Assert.Equal(0, result2.StepsCreated);
        Assert.Equal(0, result2.StepsUpdated);
        Assert.Equal(1, result2.StepsSkipped); // Soft deleted step is counted as skipped without error
        Assert.Empty(result2.Errors);

        // Confirm still only 1 step exists in database
        var totalSteps = await db.GrammarSteps.IgnoreQueryFilters().CountAsync();
        Assert.Equal(1, totalSteps);
        var existingStep = await db.GrammarSteps.IgnoreQueryFilters().FirstAsync(s => s.Code == "A1-01");
        Assert.True(existingStep.IsDeleted);
    }

    [Fact]
    public async Task ImportAsync_UnpublishedStep_RemainsUnpublishedOnReimport()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        var packSource = new FakePackSource(new GrammarContentPackDocument("A1.json", BuildPackJson(1, 1)));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        await importer.ImportAsync();
        var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
        Assert.True(step.IsPublished);

        // Unpublish step
        step.UpdateMetadata(step.TitleVi, step.TitleEn, step.SummaryVi, isPublished: false);
        await uow.CommitChangesAsync();

        // Re-import
        await importer.ImportAsync();
        var stepAfter = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
        Assert.False(stepAfter.IsPublished);
    }

    [Theory]
    [InlineData("A2", "A1-01", 1)] // Pack level A2 does not match prefix A1
    [InlineData("A1", "A1-01", 2)] // Step order 2 does not match code number 01
    public async Task ImportAsync_PrefixOrOrderMismatch_FailsValidationAndSkipsStep(string packLevel, string code, int order)
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        string packJson = $$"""
        {
          "packVersion": 1,
          "level": "{{packLevel}}",
          "steps": [
            {
              "code": "{{code}}",
              "order": {{order}},
              "titleVi": "Tiêu đề",
              "titleEn": "Title",
              "summaryVi": "Tóm tắt",
              "isLevelReview": false,
              "theoryVersion": 1,
              "theory": {
                "objectives": ["Obj"],
                "sections": [{ "heading": "H", "explanationVi": "E" }],
                "commonMistakes": [{ "wrong": "W", "right": "R", "explanationVi": "E" }],
                "summary": ["S"]
              }
            }
          ]
        }
        """;

        var packSource = new FakePackSource(new GrammarContentPackDocument("test.json", packJson));
        var importer = new GrammarContentPackImporter(db, uow, packSource);

        var result = await importer.ImportAsync();
        Assert.Equal(1, result.StepsSkipped);
        Assert.Equal(0, result.StepsCreated);
        Assert.NotEmpty(result.Errors);
    }
}
