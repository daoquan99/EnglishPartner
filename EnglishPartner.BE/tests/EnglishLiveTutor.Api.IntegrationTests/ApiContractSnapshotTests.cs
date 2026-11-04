using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests;

public class ApiContractSnapshotTests : IClassFixture<ApiTestFactory>
{
    private static readonly object ShapeFileLock = new();
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static bool _isSeeded;

    private readonly ApiTestFactory _factory;

    public ApiContractSnapshotTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static bool IsWriteMode =>
        string.Equals(Environment.GetEnvironmentVariable("API_SNAPSHOT_WRITE"), "1", StringComparison.Ordinal);

    private static string GetSnapshotsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (current.GetFiles("*.csproj").Length > 0)
            {
                return Path.Combine(current.FullName, "Snapshots");
            }
            current = current.Parent;
        }

        var cwd = Directory.GetCurrentDirectory();
        var candidate = Path.Combine(cwd, "tests", "EnglishLiveTutor.Api.IntegrationTests", "Snapshots");
        if (Directory.Exists(Path.Combine(cwd, "tests", "EnglishLiveTutor.Api.IntegrationTests")))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException("Could not find test project directory to locate Snapshots folder.");
    }

    [Fact]
    public void EndpointInventory_MatchesSnapshot()
    {
        _ = _factory.CreateClient();
        var dataSources = _factory.Services.GetServices<EndpointDataSource>();
        var endpoints = dataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        var lines = new List<string>(endpoints.Count);

        foreach (var ep in endpoints)
        {
            var httpMethods = ep.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
            var methods = (httpMethods != null && httpMethods.Count > 0)
                ? string.Join(", ", httpMethods.OrderBy(m => m, StringComparer.Ordinal))
                : "*";

            var route = ep.RoutePattern.RawText ?? "";
            if (!route.StartsWith('/'))
            {
                route = "/" + route;
            }

            string auth;
            if (ep.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                auth = "AllowAnonymous";
            }
            else
            {
                var authData = ep.Metadata.OfType<IAuthorizeData>().ToList();
                if (authData.Count > 0)
                {
                    var policies = authData
                        .Select(a => a.Policy)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct()
                        .OrderBy(p => p, StringComparer.Ordinal)
                        .ToList();

                    auth = policies.Count > 0 ? string.Join(",", policies) : "RequireAuthorization";
                }
                else
                {
                    auth = "-";
                }
            }

            var rateLimiting = ep.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            var rateLimit = !string.IsNullOrWhiteSpace(rateLimiting?.PolicyName)
                ? rateLimiting.PolicyName
                : "-";

            string permission = "-";
            foreach (var meta in ep.Metadata)
            {
                var metaType = meta.GetType();
                var prop = metaType.GetProperty("RequiredPermission", BindingFlags.Public | BindingFlags.Instance)
                        ?? metaType.GetProperty("Permission", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var val = (string?)prop.GetValue(meta);
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        permission = val;
                        break;
                    }
                }
            }

            lines.Add($"{methods} {route} | auth={auth} | rateLimit={rateLimit} | permission={permission}");
        }

        var actualLines = lines.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var snapshotDir = GetSnapshotsDirectory();
        Directory.CreateDirectory(snapshotDir);
        var snapshotPath = Path.Combine(snapshotDir, "endpoint-inventory.txt");

        if (IsWriteMode)
        {
            var content = string.Join("\n", actualLines) + "\n";
            File.WriteAllText(snapshotPath, content, Encoding.UTF8);
            return;
        }

        if (!File.Exists(snapshotPath))
        {
            Assert.Fail($"Snapshot file '{snapshotPath}' not found. Run with API_SNAPSHOT_WRITE=1 to generate snapshot.");
        }

        var expectedContent = File.ReadAllText(snapshotPath, Encoding.UTF8);
        var expectedLines = expectedContent
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var missingInActual = expectedLines.Except(actualLines, StringComparer.Ordinal).ToList();
        var addedInActual = actualLines.Except(expectedLines, StringComparer.Ordinal).ToList();

        if (missingInActual.Count > 0 || addedInActual.Count > 0)
        {
            var diff = new StringBuilder();
            diff.AppendLine($"Endpoint inventory does not match snapshot ({actualLines.Count} actual vs {expectedLines.Count} snapshot).");
            diff.AppendLine("Set API_SNAPSHOT_WRITE=1 to update snapshot.");
            if (addedInActual.Count > 0)
            {
                diff.AppendLine($"Added endpoints (+{addedInActual.Count}):");
                foreach (var line in addedInActual)
                {
                    diff.AppendLine($"+ {line}");
                }
            }
            if (missingInActual.Count > 0)
            {
                diff.AppendLine($"Missing endpoints (-{missingInActual.Count}):");
                foreach (var line in missingInActual)
                {
                    diff.AppendLine($"- {line}");
                }
            }

            Assert.Fail(diff.ToString());
        }

        Assert.Equal(expectedLines.Count, actualLines.Count);
    }

    [Fact]
    public async Task ResponseShape_Health()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("health", response);
    }

    [Fact]
    public async Task ResponseShape_AiModels()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        await EnsureSeedDataAsync(client);

        var response = await client.GetAsync("/api/ai-models");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("ai-models", response);
    }

    [Fact]
    public async Task ResponseShape_Topics()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        await EnsureSeedDataAsync(client);

        var response = await client.GetAsync("/api/topics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("topics", response);
    }

    [Fact]
    public async Task ResponseShape_Voices()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        await EnsureSeedDataAsync(client);

        var response = await client.GetAsync("/api/voices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("voices", response);
    }

    [Fact]
    public async Task ResponseShape_ProblemDetails404()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var nonExistentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var response = await client.GetAsync($"/api/topics/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("problem-details-404", response);
    }

    [Fact]
    public async Task ResponseShape_Identity_AuthMe()
    {
        var userId = Guid.NewGuid();
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var roleCode = "ROLE_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var permCode = "perm_" + Guid.NewGuid().ToString("N")[..8];
            var perm = new PermissionEntity(permCode, "Test Perm", "Identity");
            var role = new RoleEntity(roleCode, "Snapshot Role", "Role for snapshot", isSystemRole: true);
            role.GrantPermission(perm.Id);

            var user = new UserEntity(
                username: "user_" + Guid.NewGuid().ToString("N")[..8],
                email: "snapshot_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
                fullName: "Snapshot User",
                passwordHash: "dummyHash",
                id: userId);
            user.AssignRole(role.Id);

            db.Permissions.Add(perm);
            db.Roles.Add(role);
            db.Users.Add(user);
        });

        var client = _factory.CreateAuthenticatedClient(userId, role: "ADMIN");
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("identity-auth-me", response);
    }

    [Fact]
    public async Task ResponseShape_Speech_ActiveRubric()
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var rubric = new ScoringRubricEntity(
                code: "RUBRIC_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                name: "Snapshot Rubric",
                description: "Test Rubric",
                version: 1,
                assessmentType: "Pronunciation",
                isActive: true);
            rubric.AddCriterion("ACCURACY", "Accuracy", "Pronunciation accuracy", 1.0m, 1);
            db.ScoringRubrics.Add(rubric);
        });

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var response = await client.GetAsync("/api/pronunciation/rubrics/active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("speech-active-rubric", response);
    }

    [Fact]
    public async Task ResponseShape_Vocabulary_Flashcards()
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var vocab = VocabularyEntity.Create("resilience_" + Guid.NewGuid().ToString("N")[..6], topicCategory: "General");
            var sense = vocab.AddSense("noun", "capacity to recover", "kha nang phuc hoi", "B2");
            vocab.AddPronunciation("US", "/rɪˈzɪl.jəns/");
            vocab.AddWordFamily("resilient", "adjective");
            sense.AddRelation("Synonym", "toughness");
            sense.AddRelation("Antonym", "fragility");
            sense.AddCollocation("build resilience", "xay dung su kien cuong");
            sense.AddExample("Resilience is essential.", "Su kien cuong la can thiet.");
            sense.AddGrammarPattern("noun + of", "pattern explanation", "example sentence");

            db.Vocabularies.Add(vocab);
        });

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var response = await client.GetAsync("/api/vocabularies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("vocabulary-flashcards", response);
    }

    [Fact]
    public async Task ResponseShape_Lessons_List()
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var lesson = LessonEntity.Create("Lesson " + Guid.NewGuid().ToString("N")[..6], "A1", "Grammar Basics");
            db.Lessons.Add(lesson);
        });

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var response = await client.GetAsync("/api/lessons");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("lessons-list", response);
    }

    [Fact]
    public async Task ResponseShape_Grammar_Roadmap()
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            if (!db.GrammarSteps.Any(s => s.Code == "A1-01"))
            {
                var step = new GrammarStepEntity(
                    code: "A1-01",
                    cefrLevel: "A1",
                    order: 1,
                    titleVi: "Thi hien tai don",
                    titleEn: "Present Simple",
                    summaryVi: "Tom tat thi hien tai don",
                    theoryJson: "{}",
                    theoryVersion: 1,
                    isLevelReview: false,
                    isPublished: true);
                db.GrammarSteps.Add(step);
            }
        });

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var response = await client.GetAsync("/api/grammar/roadmap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("grammar-roadmap", response);
    }

    [Fact]
    public async Task ResponseShape_LiveTutor_Conversations()
    {
        var userId = Guid.NewGuid();
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var conv = new ConversationEntity(
                userId: userId.ToString(),
                id: null,
                title: "Snapshot Conversation",
                cefrLevel: "B1",
                extendedThinking: false,
                voiceName: "Aoede",
                speechSpeed: 1.0,
                conversationMode: "daily_conversation",
                topic: "General");
            db.Conversations.Add(conv);
        });

        var client = _factory.CreateAuthenticatedClient(userId, role: "LEARNER");
        var response = await client.GetAsync("/api/conversations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("livetutor-conversations", response);
    }

    [Fact]
    public async Task ResponseShape_LiveTutor_ProblemDetails404()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "ADMIN");
        var nonExistentId = "non-existent-conv-" + Guid.NewGuid().ToString("N")[..8];
        var response = await client.GetAsync($"/api/conversations/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("livetutor-problem-details-404", response);
    }

    [Fact]
    public async Task ResponseShape_Progress_Overview()
    {
        var userId = Guid.NewGuid();
        await _factory.ExecuteDbContextAsync(async db =>
        {
            if (!db.GrammarSteps.Any(s => s.Code == "A1-01"))
            {
                var step = new GrammarStepEntity(
                    code: "A1-01",
                    cefrLevel: "A1",
                    order: 1,
                    titleVi: "Thi hien tai don",
                    titleEn: "Present Simple",
                    summaryVi: "Tom tat thi hien tai don",
                    theoryJson: "{}",
                    theoryVersion: 1,
                    isLevelReview: false,
                    isPublished: true);
                db.GrammarSteps.Add(step);
            }
        });

        var client = _factory.CreateAuthenticatedClient(userId, role: "USER");
        var response = await client.GetAsync("/api/progress/overview?days=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertOrUpdateResponseShapeAsync("progress-overview", response);
    }

    private static async Task EnsureSeedDataAsync(HttpClient client)
    {
        if (_isSeeded) return;

        await SeedLock.WaitAsync();
        try
        {
            if (_isSeeded) return;

            // Seed 1 AI Model
            await client.PostAsJsonAsync("/api/ai-models", new
            {
                DisplayName = "Test Model",
                ModelCode = "test-model-1",
                CapabilityNote = "Test capability"
            });

            // Seed 1 Topic
            await client.PostAsJsonAsync("/api/topics", new
            {
                Code = "test_topic",
                Name = "Test Topic",
                DisplayNameVi = "Chu de test",
                Category = "General"
            });

            // Seed 1 Voice
            await client.PostAsJsonAsync("/api/voices", new
            {
                Name = "test-voice",
                DisplayName = "Test Voice",
                Label = "Test",
                Type = "prebuilt",
                Gender = "Female",
                Persona = "Friendly"
            });

            _isSeeded = true;
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static async Task AssertOrUpdateResponseShapeAsync(string key, HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var actualShape = ExtractShape(doc.RootElement);

        var snapshotDir = GetSnapshotsDirectory();
        Directory.CreateDirectory(snapshotDir);
        var snapshotPath = Path.Combine(snapshotDir, "response-shapes.json");

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        lock (ShapeFileLock)
        {
            var dict = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            if (File.Exists(snapshotPath))
            {
                try
                {
                    var existingJson = File.ReadAllText(snapshotPath, Encoding.UTF8);
                    using var existingDoc = JsonDocument.Parse(existingJson);
                    if (existingDoc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in existingDoc.RootElement.EnumerateObject())
                        {
                            dict[prop.Name] = ExtractShape(prop.Value);
                        }
                    }
                }
                catch
                {
                    // If file is invalid/empty, will be overwritten in write mode
                }
            }

            if (IsWriteMode)
            {
                dict[key] = actualShape;
                var updatedJson = JsonSerializer.Serialize(dict, serializerOptions) + "\n";
                File.WriteAllText(snapshotPath, updatedJson, Encoding.UTF8);
                return;
            }

            if (!dict.TryGetValue(key, out var expectedShape))
            {
                Assert.Fail($"Snapshot for key '{key}' not found in '{snapshotPath}'. Run with API_SNAPSHOT_WRITE=1 to update.");
            }

            var expectedJson = JsonSerializer.Serialize(expectedShape, serializerOptions);
            var actualJson = JsonSerializer.Serialize(actualShape, serializerOptions);

            Assert.Equal(expectedJson, actualJson);
        }
    }

    private static object? ExtractShape(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ExtractShape(prop.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                if (element.GetArrayLength() > 0)
                {
                    var first = element.EnumerateArray().First();
                    if (first.ValueKind == JsonValueKind.Object)
                    {
                        var merged = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in item.EnumerateObject())
                                {
                                    if (!merged.ContainsKey(prop.Name))
                                    {
                                        merged[prop.Name] = ExtractShape(prop.Value);
                                    }
                                }
                            }
                        }
                        list.Add(merged);
                    }
                    else
                    {
                        list.Add(ExtractShape(first));
                    }
                }
                return list;

            default:
                return null;
        }
    }
}
