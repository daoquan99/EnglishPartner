using System.Net;
using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarExerciseAiGeneratorAdapterTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return _responseFactory(request);
        }
    }

    private sealed class FakeDbFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public FakeDbFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IAppDbContext>(new AppDbContext(_options));
        }
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    [Fact]
    public async Task GenerateRawAsync_SendsApiKeyViaHeader_NeverInUrlQuery()
    {
        string secretApiKey = "SECRET_GEMINI_KEY_12345";
        var options = CreateInMemoryOptions();
        await using (var db = new AppDbContext(options))
        {
            db.AiModelCatalog.Add(new AiModelConfigEntity(
                displayName: "Gemini 3.8 Flash",
                modelCode: "gemini-3.8-flash",
                supportsTextGeneration: true));
            await db.SaveChangesAsync();
        }

        var handler = new MockHttpMessageHandler(req =>
        {
            var fakeGeminiResponse = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new { text = "{\"exercises\": []}" }
                            }
                        }
                    }
                }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(fakeGeminiResponse), System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var generator = new GeminiGrammarExerciseAiGenerator(httpClient, new FakeDbFactory(options));

        var config = new GrammarAiExecutionConfig(ApiKey: secretApiKey, ModelCode: "gemini-3.8-flash");
        string raw = await generator.GenerateRawAsync("Please generate exercises", config);

        Assert.Equal("{\"exercises\": []}", raw);
        Assert.NotNull(handler.LastRequest);

        // Verify URL does NOT have ?key=
        Assert.False(handler.LastRequest.RequestUri!.Query.Contains("key="), "API key must NOT appear in URL query string.");

        // Verify header x-goog-api-key exists
        Assert.True(handler.LastRequest.Headers.Contains("x-goog-api-key"), "API key must be sent in 'x-goog-api-key' header.");
        var headerValues = handler.LastRequest.Headers.GetValues("x-goog-api-key");
        Assert.Contains(secretApiKey, headerValues);
    }

    [Fact]
    public async Task GenerateRawAsync_WhenProviderErrors_KeyDoesNotAppearInExceptionMessage()
    {
        string secretApiKey = "VERY_CONFIDENTIAL_KEY_999";
        var options = CreateInMemoryOptions();
        await using (var db = new AppDbContext(options))
        {
            db.AiModelCatalog.Add(new AiModelConfigEntity(
                displayName: "Gemini 3.8 Flash",
                modelCode: "gemini-3.8-flash",
                supportsTextGeneration: true));
            await db.SaveChangesAsync();
        }

        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\": {\"message\": \"Invalid argument\"}}")
            };
        });

        var httpClient = new HttpClient(handler);
        var generator = new GeminiGrammarExerciseAiGenerator(httpClient, new FakeDbFactory(options));

        var config = new GrammarAiExecutionConfig(ApiKey: secretApiKey, ModelCode: "gemini-3.8-flash");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generator.GenerateRawAsync("Test prompt", config));

        Assert.DoesNotContain(secretApiKey, ex.Message);
    }
}
