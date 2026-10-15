using System.Net;
using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishLiveTutor.Domain.Tests;

public class ManualVocabularyGenerationTests
{
    private const string Sample = """
        [{
          "word": "resilient", "partOfSpeech": "adjective", "definitionEn": "Able to recover quickly",
          "meaningVi": "Kiên cường", "descriptionEn": "Useful at work", "ipaUs": "/rɪˈzɪliənt/", "ipaUk": "/rɪˈzɪliənt/",
          "synonyms": ["strong"], "antonyms": ["fragile"],
          "wordFamilies": [{"relatedWord": "resilience", "partOfSpeech": "noun"}],
          "collocations": [{"phraseEn": "resilient team", "meaningVi": "đội ngũ kiên cường"}],
          "examples": [{"sentenceEn": "She is resilient.", "sentenceVi": "Cô ấy kiên cường."}],
          "grammarPatterns": [{"pattern": "be resilient", "explanation": "be + adjective", "example": "We are resilient."}]
        }]
        """;

    private sealed class TestDbFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        public AppDbContext Create() => new(_options);
        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult<IAppDbContext>(Create());
    }

    private static ImportVocabularyJsonCommandHandler Importer(TestDbFactory factory) => new(
        new VocabularyGenerationContent(), new VocabularyGenerationWriter(factory, NullLogger<VocabularyGenerationWriter>.Instance));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Parser_ShouldHandlePlainAndFencedJson(bool fenced)
    {
        var content = new VocabularyGenerationContent();
        var items = content.ParseResponse(fenced ? "```json\r\n" + Sample + "\r\n```" : Sample, "b2");
        var item = Assert.Single(items);
        Assert.Equal("resilient", item.Word);
        Assert.Equal("B2", item.CefrLevel);
        Assert.Equal("resilience", item.WordFamilies.Single().RelatedWord);
        Assert.Equal("Cô ấy kiên cường.", item.Examples.Single().SentenceVi);
        Assert.Equal("be resilient", item.GrammarPatterns.Single().Pattern);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not JSON")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[123]")]
    [InlineData("[{\"word\": \"test\"}]")]
    [InlineData("[{\"word\": 123, \"definitionEn\": \"meaning\", \"meaningVi\": \"nghĩa\"}]")]
    [InlineData("[{\"word\": \"test\", \"definitionEn\": \"meaning\", \"meaningVi\": \"nghĩa\", \"synonyms\": {}}]")]
    [InlineData("```json\n[]")]
    public void Parser_ShouldRejectInvalidResponses(string json) =>
        Assert.Throws<InvalidOperationException>(() => new VocabularyGenerationContent().ParseResponse(json, "B1"));

    [Fact]
    public async Task PromptQuery_ShouldIncludeSavedAndExplicitExclusions_WithoutWritingData()
    {
        var factory = new TestDbFactory();
        await using (var db = factory.Create())
        {
            db.Vocabularies.Add(VocabularyEntity.Create("already saved"));
            await db.SaveChangesAsync();
        }
        var result = await new BuildVocabularyGenerationPromptQueryHandler(factory, new VocabularyGenerationContent())
            .HandleAsync(new(new(RequestedCount: 3, CefrLevel: "b2", Topic: " Work ", ExcludedWords: ["Custom", " custom "])));
        Assert.Contains("Generate 3 distinct", result.Prompt);
        Assert.Contains("CEFR level B2", result.Prompt);
        Assert.Contains("'Work'", result.Prompt);
        Assert.Contains("already saved", result.Prompt);
        Assert.Contains("custom", result.ExcludedWords);
        Assert.Equal(2, result.ExcludedWords.Count);
        await using var verify = factory.Create();
        Assert.Single(await verify.Vocabularies.ToListAsync());
        Assert.Empty(await verify.VocabularyGenerationAudits.ToListAsync());
    }

    [Fact]
    public async Task Import_ShouldSaveFullGraphAndCreateTodaysPlan_WithoutAnApiKeyOrCatalogModel()
    {
        var factory = new TestDbFactory();
        var result = await Importer(factory).HandleAsync(new("learner-a",
            new("```json\n" + Sample + "\n```", ModelCode: "my-web-model", CefrLevel: "B2", Topic: "Work")));
        var card = Assert.Single(result);
        Assert.Equal("resilient", card.Word);
        Assert.Equal(2, card.Pronunciations.Count);
        Assert.Single(card.WordFamilies);
        Assert.Single(card.Collocations);
        Assert.Single(card.Examples);
        Assert.Single(card.GrammarPatterns);
        await using var db = factory.Create();
        var vocabulary = await db.Vocabularies.SingleAsync();
        Assert.Equal("my-web-model", vocabulary.SourceModelId);
        Assert.Equal("Work", vocabulary.TopicCategory);
        var plan = await db.DailyVocabularyPlans.Include(p => p.Items).SingleAsync();
        Assert.Equal("learner-a", plan.UserId);
        Assert.Equal(card.SenseId, plan.Items.Single().SenseId);
        var audit = await db.VocabularyGenerationAudits.SingleAsync();
        Assert.Equal("Completed", audit.Status);
        Assert.Equal(1, audit.AcceptedCount);
        Assert.Equal("learner-a", audit.UserId);
        Assert.Empty(await db.AiModelCatalog.ToListAsync());
    }

    [Fact]
    public async Task Reimport_ShouldReuseSenseAndPlanItem_IncludingDuplicatesWithinOneResponse()
    {
        var factory = new TestDbFactory();
        var importer = Importer(factory);
        string duplicates = "[" + Sample.Trim()[1..^1] + "," + Sample.Trim()[1..^1] + "]";
        var request = new ImportVocabularyJsonRequest(duplicates, CefrLevel: "B2");
        var first = Assert.Single(await importer.HandleAsync(new("learner-a", request)));
        var second = Assert.Single(await importer.HandleAsync(new("learner-a", request)));
        Assert.Equal(first.VocabularyId, second.VocabularyId);
        Assert.Equal(first.SenseId, second.SenseId);
        await using var db = factory.Create();
        Assert.Single(await db.Vocabularies.ToListAsync());
        Assert.Single(await db.VocabularySenses.ToListAsync());
        Assert.Single(await db.DailyVocabularyItems.ToListAsync());
        Assert.Equal(2, await db.VocabularyPronunciations.CountAsync());
        Assert.Single(await db.VocabularyExamples.ToListAsync());
        var audits = await db.VocabularyGenerationAudits.OrderBy(a => a.AcceptedCount).ToListAsync();
        Assert.Equal(0, audits[0].AcceptedCount);
        Assert.Equal(1, audits[1].AcceptedCount);
    }

    [Fact]
    public async Task Import_ShouldKeepDistinctSensesAndRespectAssignmentFlag()
    {
        var factory = new TestDbFactory();
        var importer = Importer(factory);
        await importer.HandleAsync(new("learner-a", new(Sample, AssignToTodayPlan: false)));
        await importer.HandleAsync(new("learner-a", new(Sample.Replace("Able to recover quickly", "Able to withstand difficulties"), AssignToTodayPlan: false)));
        await using var db = factory.Create();
        Assert.Single(await db.Vocabularies.ToListAsync());
        Assert.Equal(2, await db.VocabularySenses.CountAsync());
        Assert.Empty(await db.DailyVocabularyPlans.ToListAsync());
    }

    [Fact]
    public async Task InvalidSecondItem_ShouldNotSaveTheFirstItem()
    {
        var factory = new TestDbFactory();
        string invalid = "[" + Sample.Trim()[1..^1] + ", {\"word\": \"incomplete\"}]";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Importer(factory).HandleAsync(new("learner-a", new(invalid))));
        await using var db = factory.Create();
        Assert.Empty(await db.Vocabularies.ToListAsync());
        Assert.Empty(await db.VocabularySenses.ToListAsync());
        Assert.Empty(await db.DailyVocabularyPlans.ToListAsync());
        Assert.Equal("Failed", (await db.VocabularyGenerationAudits.SingleAsync()).Status);
    }

    private sealed class FakeGenerator : IVocabularyAiGenerator
    {
        public int Calls { get; private set; }
        public Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(string apiKey, string modelCode,
            string cefrLevel, string topic, int count, List<string> excludedWords, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new VocabularyGenerationContent().ParseResponse(Sample, cefrLevel));
        }
    }

    [Fact]
    public async Task OversizedNestedField_ShouldReturnValidationErrorWithoutSavingVocabulary()
    {
        var factory = new TestDbFactory();
        string json = Sample.Replace("be + adjective", new string('x', 601));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Importer(factory).HandleAsync(new("learner-a", new(json))));
        Assert.Contains("grammarPatterns.explanation", error.Message);
        await using var db = factory.Create();
        Assert.Empty(await db.Vocabularies.ToListAsync());
    }

    [Fact]
    public async Task AutomaticGeneration_ShouldStillUseTheProviderAndSharedPersistence()
    {
        var factory = new TestDbFactory();
        await using (var db = factory.Create())
        {
            db.AiModelCatalog.Add(new AiModelConfigEntity("Test", "test-model", supportsTextGeneration: true));
            await db.SaveChangesAsync();
        }
        var generator = new FakeGenerator();
        var handler = new GenerateVocabularyCommandHandler(factory, generator,
            new BuildVocabularyGenerationPromptQueryHandler(factory, new VocabularyGenerationContent()),
            new VocabularyGenerationWriter(factory, NullLogger<VocabularyGenerationWriter>.Instance));
        var result = await handler.HandleAsync(new("learner-a", new("test-key", "test-model", CefrLevel: "B2")));
        Assert.Single(result);
        Assert.Equal(1, generator.Calls);
        await using var verify = factory.Create();
        Assert.Single(await verify.VocabularySenses.ToListAsync());
        Assert.Equal("Completed", (await verify.VocabularyGenerationAudits.SingleAsync()).Status);
    }

    private sealed class FakeGoogleHandler(string response) : HttpMessageHandler
    {
        public string? Prompt { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Prompt = payload.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            var body = JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = response } } } } } });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }

    [Fact]
    public async Task GoogleGenerator_ShouldUseTheSamePromptAndParserAsManualGeneration()
    {
        using var transport = new FakeGoogleHandler("```json\n" + Sample + "\n```");
        using var http = new HttpClient(transport);
        var content = new VocabularyGenerationContent();
        var generator = new GeminiVocabularyAiGenerator(http, content);
        var result = await generator.GenerateVocabularyAsync("test-key", "test-model", "B2", "Work", 3, ["known"]);
        Assert.Equal(content.BuildPrompt("B2", "Work", 3, ["known"]), transport.Prompt);
        Assert.Equal("resilient", Assert.Single(result).Word);
    }
}
