using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Modules.LiveTutor.Tests;

public class GrammarCorrectionAnalyzerTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_WhenValidJsonReturned_ParsesIssuesAndFeedback()
    {
        string json = """
        {
          "candidates": [{
            "content": {
              "parts": [{
                "text": "{\"status\":\"has_errors\",\"originalText\":\"I goes to market yesterday\",\"correctedText\":\"I went to the market yesterday\",\"improvedText\":\"I headed over to the grocery store yesterday\",\"errorCategory\":\"Verb Tense\",\"explanationVi\":\"Cần dùng thì quá khứ đơn 'went'.\",\"cefrTip\":\"Ôn thì quá khứ\",\"issues\":[{\"original\":\"goes\",\"fix\":\"went\",\"category\":\"Tense\",\"explanationVi\":\"Quá khứ của go là went\"}]}"
              }]
            }
          }],
          "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 20, "totalTokenCount": 30 }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        var result = await analyzer.AnalyzeUserUtteranceAsync("fake-api-key", "conv-1", "turn-1", "I goes to market yesterday", "B1");

        Assert.NotNull(result);
        Assert.Equal("has_errors", result.Status);
        Assert.Equal("I went to the market yesterday", result.CorrectedText);
        Assert.Equal("I headed over to the grocery store yesterday", result.ImprovedText);
        Assert.Single(result.Issues);
        Assert.Equal("goes", result.Issues[0].Original);
        Assert.Equal("went", result.Issues[0].Fix);
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_WhenCorrectedSentenceMatchesOriginal_ClearsCorrectedText()
    {
        string json = """
        {
          "candidates": [{
            "content": {
              "parts": [{
                "text": "{\"status\":\"has_errors\",\"originalText\":\"I love reading books.\",\"correctedText\":\"I love reading books\",\"improvedText\":\"I really enjoy reading books in my free time.\",\"errorCategory\":\"Style\",\"explanationVi\":\"Tốt\",\"cefrTip\":\"Tip\",\"issues\":[]}"
              }]
            }
          }],
          "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 20, "totalTokenCount": 30 }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        var result = await analyzer.AnalyzeUserUtteranceAsync("fake-api-key", "conv-1", "turn-1", "I love reading books.", "B1");

        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.CorrectedText));
        Assert.Equal("can_improve", result.Status);
        Assert.Equal("I really enjoy reading books in my free time.", result.ImprovedText);
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_WhenBothCorrectedAndImprovedMatchOriginal_SetsStatusAlreadyNatural()
    {
        string json = """
        {
          "candidates": [{
            "content": {
              "parts": [{
                "text": "{\"status\":\"has_errors\",\"originalText\":\"That is amazing!\",\"correctedText\":\"That is amazing\",\"improvedText\":\"that is amazing.\",\"errorCategory\":\"None\",\"explanationVi\":\"Hoàn hảo\",\"cefrTip\":\"Tip\",\"issues\":[]}"
              }]
            }
          }],
          "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 20, "totalTokenCount": 30 }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        var result = await analyzer.AnalyzeUserUtteranceAsync("fake-api-key", "conv-1", "turn-1", "That is amazing!", "B1");

        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.CorrectedText));
        Assert.True(string.IsNullOrEmpty(result.ImprovedText));
        Assert.Equal("already_natural", result.Status);
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_WhenAiThrowsException_ReturnsAnalysisFailedStatus_AndDoesNotFabricateFakeCorrection()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"Internal Server Error\"}", Encoding.UTF8, "application/json")
        });

        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        var result = await analyzer.AnalyzeUserUtteranceAsync("fake-api-key", "conv-1", "turn-1", "I am very tired today after work", "B1");

        Assert.NotNull(result);
        Assert.Equal("analysis_failed", result.Status);
        Assert.Equal("I am very tired today after work", result.OriginalText);
        Assert.True(string.IsNullOrEmpty(result.CorrectedText));
        Assert.True(string.IsNullOrEmpty(result.ImprovedText));
        Assert.True(result.Issues == null || result.Issues.Count == 0);
    }

    [Fact]
    public void TryExtractCorrectionFromTeacherReply_WhenExtractedTextMatchesOriginal_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        string teacherReply = "You should say: \"I like playing tennis.\"";
        string userUtterance = "I like playing tennis";

        var result = analyzer.TryExtractCorrectionFromTeacherReply(
            "conv-1", "turn-1", userUtterance, teacherReply, "B1");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_GeneratesRequestWithResponseMimeTypeAndResponseSchemaAndDelimitedPrompt()
    {
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            string json = """
            {
              "candidates": [{
                "content": {
                  "parts": [{
                    "text": "{\"status\":\"has_errors\",\"originalText\":\"spoofed text\",\"correctedText\":\"I went to the store\",\"improvedText\":\"I went to the store\",\"errorCategory\":\"Grammar\",\"explanationVi\":\"Giải thích\",\"cefrTip\":\"Tip\",\"issues\":[]}"
                  }]
                }
              }],
              "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 20, "totalTokenCount": 30 }
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        var result = await analyzer.AnalyzeUserUtteranceAsync(
            "test-key",
            "conv-1",
            "turn-1",
            "I goes to the store",
            "B1",
            lastAiTeacherReply: "Where did you go?",
            recentTurnsContext: "Teacher: Where did you go?\nStudent: I goes to the store");

        Assert.NotNull(capturedBody);
        var node = JsonNode.Parse(capturedBody);
        Assert.NotNull(node);

        // Verify responseMimeType and responseSchema are configured
        var genConfig = node["generationConfig"];
        Assert.NotNull(genConfig);
        Assert.Equal("application/json", genConfig["responseMimeType"]?.GetValue<string>());
        Assert.NotNull(genConfig["responseSchema"]);

        // Verify prompt has delimiter preventing prompt injection
        string? prompt = node["contents"]?[0]?["parts"]?[0]?["text"]?.GetValue<string>();
        Assert.NotNull(prompt);
        Assert.Contains("=== BEGIN DATA", prompt);
        Assert.Contains("=== END DATA ===", prompt);
        Assert.Contains("Where did you go?", prompt);

        // Verify OriginalText is always the server's cleaned transcript, not the model's spoofed text
        Assert.NotNull(result);
        Assert.Equal("I goes to the store", result.OriginalText);
    }

    [Fact]
    public async Task AnalyzeUserUtteranceAsync_DeterministicRule_SubstitutesRegexGroupsCorrectlyWithoutLiteralDollar()
    {
        // When AI fails (500), the analyzer falls back to deterministic rule analysis
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var client = new HttpClient(handler);
        var translator = new FastTextTranslatorFallback(client);
        var analyzer = new GrammarCorrectionAnalyzerService(handler, translator);

        // "two month" triggers rule: (\b(two|three|...)\s+(time|month|...)\b) -> "$1 $2s"
        var result = await analyzer.AnalyzeUserUtteranceAsync("test-key", "conv-1", "turn-1", "I stayed there for two month.", "B1");

        Assert.NotNull(result);
        Assert.Equal("has_errors", result.Status);
        Assert.Contains("two months", result.CorrectedText);
        Assert.NotNull(result.Issues);
        Assert.NotEmpty(result.Issues);

        var issue = result.Issues[0];
        Assert.DoesNotContain("$", issue.Fix);
        Assert.Equal("two months", issue.Fix);
    }
}
