using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Application.Common.Validation;
using EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.DraftOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ExerciseOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.MediaOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Application.Features.Lessons.Queries.MediaQueryOperations;
using EnglishLiveTutor.Application.Features.Lessons.Queries.RolePlayQueries;
using EnglishLiveTutor.Application.Features.Lessons.Validation;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Entities;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonFeatureEnhancementTests
{
    private static (AppDbContext Db, FakeTimeProvider Clock, FakeCurrentUser User, UnitOfWork Uow, IdempotencyService Idempotency) CreateContext(string? userId = "test-user-1")
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var user = new FakeCurrentUser { UserId = userId };
        var interceptor = new AuditingSaveChangesInterceptor(clock, user);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptor)
            .Options;

        var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var idempotency = new IdempotencyService(db, clock, NullLogger<IdempotencyService>.Instance);
        return (db, clock, user, uow, idempotency);
    }

    // =========================================================================
    // 1. Lesson Media Endpoint & Real Storage Tests
    // =========================================================================

    [Fact]
    public async Task UploadLessonMedia_StoresBytes_AndReturnsMetadata()
    {
        var (db, clock, user, uow, _) = CreateContext("author-user");
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var handler = new UploadLessonMediaCommandHandler(audioRepo, mediaStorage, user, uow);

        var fileBytes = Encoding.UTF8.GetBytes("Fake audio content header");
        using var stream = new MemoryStream(fileBytes);

        var command = new UploadLessonMediaCommand(stream, "sample.mp3", "audio/mpeg", 2500);
        var result = await handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AssetId);
        Assert.Equal("sample.mp3", result.FileName);
        Assert.Equal("audio/mpeg", result.ContentType);
        Assert.True(result.FileSizeBytes > 0);

        // Verify entity persisted in DB
        var assetGuid = Guid.Parse(result.AssetId);
        var persisted = await db.AudioRecordings.FindAsync(assetGuid);
        Assert.NotNull(persisted);
        Assert.Equal("author-user", persisted.UserId);

        // Verify storage has the file
        var exists = await mediaStorage.ExistsAsync(persisted.StorageKey);
        Assert.True(exists);
    }

    [Fact]
    public async Task GetLessonMediaStream_WhenDifferentUser_ThrowsNotFoundException()
    {
        var (db, clock, user, uow, _) = CreateContext("author-user");
        var mediaStorage = new FakeLessonMediaStorage();
        var audioRepo = new AudioRecordingRepository(db);

        // Create recording owned by author-user
        var recording = new AudioRecordingEntity(
            userId: "author-user",
            storageKey: "media/test.mp3",
            contentType: "audio/mpeg",
            fileSizeBytes: 100,
            durationMilliseconds: 5000);
        db.AudioRecordings.Add(recording);
        await db.SaveChangesAsync();

        // Query with different user -> throws NotFoundException to prevent data enumeration
        var queryUser = new FakeCurrentUser { UserId = "different-user" };
        var queryHandler = new GetLessonMediaStreamQueryHandler(audioRepo, mediaStorage, queryUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            queryHandler.HandleAsync(new GetLessonMediaStreamQuery(recording.Id)));
    }

    [Fact]
    public async Task GetLessonMediaStream_WhenFileMissingInStorage_ThrowsNotFoundException()
    {
        var (db, clock, user, uow, _) = CreateContext("author-user");
        var mediaStorage = new FakeLessonMediaStorage(); // Empty storage
        var audioRepo = new AudioRecordingRepository(db);

        var recording = new AudioRecordingEntity(
            userId: "author-user",
            storageKey: "media/non_existent.mp3",
            contentType: "audio/mpeg",
            fileSizeBytes: 100,
            durationMilliseconds: 5000);
        db.AudioRecordings.Add(recording);
        await db.SaveChangesAsync();

        var queryHandler = new GetLessonMediaStreamQueryHandler(audioRepo, mediaStorage, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            queryHandler.HandleAsync(new GetLessonMediaStreamQuery(recording.Id)));
    }

    [Fact]
    public async Task UploadLessonMedia_WithInvalidExtension_ThrowsValidationException()
    {
        var (db, clock, user, uow, _) = CreateContext("author-user");
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var handler = new UploadLessonMediaCommandHandler(audioRepo, mediaStorage, user, uow);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("evil payload"));
        var command = new UploadLessonMediaCommand(stream, "script.exe", "application/x-msdownload", 100);

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command));
    }

    // =========================================================================
    // 2. Persistent Role-Play Aggregate Tests
    // =========================================================================

    [Fact]
    public async Task StartRolePlaySession_CreatesPersistentSessionInDatabase()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("learner-alice");
        var rolePlayRepo = new LessonRolePlayRepository(db);
        var handler = new StartRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);

        var command = new StartRolePlaySessionCommand(
            Scenario: "Ordering Coffee in London",
            AiRole: "Barista",
            InitialGreeting: "Hello! What can I get for you today?",
            MaxTurns: 6);

        var result = await handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Barista", result.AiRole);
        Assert.Equal("Hello! What can I get for you today?", result.InitialMessage);

        // Verify in DB
        var persisted = await rolePlayRepo.GetByIdAsync(result.SessionId);
        Assert.NotNull(persisted);
        Assert.Equal("learner-alice", persisted.UserId);
        Assert.Equal(6, persisted.MaxTurns);
    }

    [Fact]
    public async Task SendRolePlayTurn_AppendsUserAndAssistantTurn_IncrementsTurnIndex()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("learner-alice");
        var rolePlayRepo = new LessonRolePlayRepository(db);
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var startHandler = new StartRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);

        var sessionResult = await startHandler.HandleAsync(new StartRolePlaySessionCommand(
            Scenario: "Checking into Hotel",
            AiRole: "Receptionist",
            InitialGreeting: "Good evening!",
            MaxTurns: 4));

        var fakeAiReplyProvider = new FakeRolePlayReplyProvider
        {
            ReplyFunc = (scenario, role, hist, turn, max) => "Welcome to the Grand Hotel! May I have your name, please?"
        };

        var turnHandler = new SendRolePlayTurnCommandHandler(rolePlayRepo, audioRepo, mediaStorage, fakeAiReplyProvider, user, clock, uow, idempotency);

        var turnResult = await turnHandler.HandleAsync(new SendRolePlayTurnCommand(
            SessionId: sessionResult.SessionId,
            ClientTurnIndex: 1,
            UserMessage: "Hi, I have a reservation under Smith."));

        Assert.Equal(1, turnResult.TurnIndex);
        Assert.Equal("Welcome to the Grand Hotel! May I have your name, please?", turnResult.AiTurn.Message);
        Assert.Equal("Hi, I have a reservation under Smith.", turnResult.UserTurn.Message);
    }

    [Fact]
    public async Task SendRolePlayTurn_WhenDifferentUser_ThrowsNotFoundException()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("learner-alice");
        var rolePlayRepo = new LessonRolePlayRepository(db);
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var startHandler = new StartRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);

        var session = await startHandler.HandleAsync(new StartRolePlaySessionCommand("Interview", "Interviewer", null, 4));

        // Different user tries to send turn -> throws NotFoundException to prevent data enumeration
        var bob = new FakeCurrentUser { UserId = "learner-bob" };
        var turnHandler = new SendRolePlayTurnCommandHandler(rolePlayRepo, audioRepo, mediaStorage, new FakeRolePlayReplyProvider(), bob, clock, uow, idempotency);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            turnHandler.HandleAsync(new SendRolePlayTurnCommand(session.SessionId, 1, "Hello")));
    }

    [Fact]
    public async Task SendRolePlayTurn_WhenTurnIndexStale_ThrowsInvalidOperationException()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("learner-alice");
        var rolePlayRepo = new LessonRolePlayRepository(db);
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var startHandler = new StartRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);
        var session = await startHandler.HandleAsync(new StartRolePlaySessionCommand("Job Interview", "Manager", null, 4));

        var turnHandler = new SendRolePlayTurnCommandHandler(rolePlayRepo, audioRepo, mediaStorage, new FakeRolePlayReplyProvider(), user, clock, uow, idempotency);

        // Turn index is 0 (greeting), but client passes turn index 99
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            turnHandler.HandleAsync(new SendRolePlayTurnCommand(session.SessionId, 99, "Hello")));
    }

    [Fact]
    public async Task EndRolePlaySession_PreservesTranscriptInDatabase()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("learner-alice");
        var rolePlayRepo = new LessonRolePlayRepository(db);
        var audioRepo = new AudioRecordingRepository(db);
        var mediaStorage = new FakeLessonMediaStorage();
        var startHandler = new StartRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);
        var session = await startHandler.HandleAsync(new StartRolePlaySessionCommand("Doctor visit", "Doctor", null, 4));

        var turnHandler = new SendRolePlayTurnCommandHandler(rolePlayRepo, audioRepo, mediaStorage, new FakeRolePlayReplyProvider(), user, clock, uow, idempotency);
        await turnHandler.HandleAsync(new SendRolePlayTurnCommand(session.SessionId, 1, "I have a headache."));

        var endHandler = new EndRolePlaySessionCommandHandler(rolePlayRepo, user, clock, uow, idempotency);
        var endResult = await endHandler.HandleAsync(new EndRolePlaySessionCommand(session.SessionId));

        Assert.Equal("SessionEnded", endResult.Status);
        Assert.Equal(3, endResult.Transcript.Count); // 1 greeting + 1 user + 1 assistant turn preserved

        // Query session after end
        var queryHandler = new GetRolePlaySessionQueryHandler(rolePlayRepo, user);
        var queried = await queryHandler.HandleAsync(new GetRolePlaySessionQuery(session.SessionId));
        Assert.NotNull(queried);
        Assert.Equal("SessionEnded", queried.Status);
        Assert.Equal(3, queried.Transcript.Count);
    }

    // =========================================================================
    // 3. Prompt Generation & Candidate Validation Tests
    // =========================================================================

    [Fact]
    public async Task LessonPromptBuilder_IncludesRequestedTypesAndExcludesOthers()
    {
        var templateProvider = new LessonPromptTemplateProvider();
        var builder = new LessonPromptBuilder(templateProvider);

        var input = new LessonPromptInput(
            Topic: "Ordering at a Restaurant",
            CefrLevel: "B1",
            LessonTitle: "B1 Dining Out",
            PreferredExerciseTypes: new List<string> { "MultipleChoice", "Flashcards" },
            TargetExerciseCount: 4,
            CustomInstructions: "Include vegetarian options.");

        var promptResult = await builder.BuildPromptAsync(input);

        Assert.NotNull(promptResult);
        Assert.Contains("MultipleChoice", promptResult.UserPrompt);
        Assert.Contains("Flashcards", promptResult.UserPrompt);
        Assert.DoesNotContain("Dictation", promptResult.UserPrompt);
        Assert.DoesNotContain("Shadowing", promptResult.UserPrompt);
        Assert.Equal("lesson-exercises.v1", promptResult.SchemaVersion);
    }

    [Fact]
    public async Task LessonCandidateValidator_WhenCorrectOptionIdNotFoundInOptions_FailsValidation()
    {
        var (db, _, _, _, _) = CreateContext();
        var validator = new LessonCandidateValidator(db);

        var candidateJson = @"{
            ""schemaVersion"": ""lesson-exercises.v1"",
            ""lesson"": { ""title"": ""Test Lesson"", ""cefrLevel"": ""A2"", ""topic"": ""Grammar"" },
            ""exercises"": [
                {
                    ""key"": ""ex_01"",
                    ""type"": ""MultipleChoice"",
                    ""title"": ""Grammar MC"",
                    ""content"": {
                        ""options"": [
                            { ""id"": ""opt_a"", ""text"": ""Alpha"" },
                            { ""id"": ""opt_b"", ""text"": ""Beta"" }
                        ]
                    },
                    ""grading"": {
                        ""correctOptionIds"": [""opt_z""]
                    }
                }
            ]
        }";

        var node = JsonNode.Parse(candidateJson);
        var validation = await validator.ValidateCandidateAsync(node);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, i => i.Severity == "Error" && i.Message.Contains("opt_z"));
    }

    [Fact]
    public async Task LessonCandidateValidator_WhenRubricCodeDoesNotExistInDb_FailsValidation()
    {
        var (db, _, _, _, _) = CreateContext();
        var validator = new LessonCandidateValidator(db);

        var candidateJson = @"{
            ""schemaVersion"": ""lesson-exercises.v1"",
            ""lesson"": { ""title"": ""Test Lesson"", ""cefrLevel"": ""B2"", ""topic"": ""Writing"" },
            ""exercises"": [
                {
                    ""key"": ""ex_essay"",
                    ""type"": ""WritingPractice"",
                    ""title"": ""Opinion Essay"",
                    ""content"": { ""prompt"": ""Write about technology"" },
                    ""grading"": {
                        ""rubricCode"": ""NON_EXISTENT_RUBRIC_CODE""
                    }
                }
            ]
        }";

        var node = JsonNode.Parse(candidateJson);
        var validation = await validator.ValidateCandidateAsync(node);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, i => i.Severity == "Error" && i.Message.Contains("NON_EXISTENT_RUBRIC_CODE"));
    }

    [Fact]
    public async Task CommitLessonImport_WhenCandidateIsInvalid_ThrowsValidationException()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("teacher-author");
        var sanitizer = new LessonSanitizer();
        var getByIdHandler = new GetLessonByIdQueryHandler(db, sanitizer);
        var validator = new LessonCandidateValidator(db);
        var commitHandler = new CommitLessonImportCommandHandler(db, uow, validator, getByIdHandler, user, idempotency);

        // Invalid candidate: multiple choice with missing correctOptionId match
        var invalidCandidateJson = @"{
            ""schemaVersion"": ""lesson-exercises.v1"",
            ""lesson"": { ""title"": ""Invalid Lesson"", ""cefrLevel"": ""A1"", ""topic"": ""Test"" },
            ""exercises"": [
                {
                    ""key"": ""ex_invalid"",
                    ""type"": ""MultipleChoice"",
                    ""title"": ""Broken Quiz"",
                    ""content"": { ""options"": [ { ""id"": ""opt_1"", ""text"": ""One"" } ] },
                    ""grading"": { ""correctOptionIds"": [""opt_999""] }
                }
            ]
        }";

        var node = JsonNode.Parse(invalidCandidateJson)!;
        var request = new CommitLessonImportRequest(null, null, "Append", node);
        var command = new CommitLessonImportCommand(request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => commitHandler.HandleAsync(command));
    }

    // =========================================================================
    // 4. AI Grader & Attempt Operations Tests
    // =========================================================================

    [Fact]
    public async Task RetryAnswerGrading_WhenNotOwner_ThrowsForbiddenException()
    {
        var (db, clock, user, uow, idempotency) = CreateContext("student-alice");
        var lesson = LessonEntity.Create("Speaking Lesson", "B1", "Travel");
        var exercise = lesson.AddExercise("ex-speak", ExerciseType.SpeakingPractice, "Speaking Task", "Speak about holidays", 10,
            GradingMode.AiRubric, "{\"prompt\":\"Describe holiday\"}");
        var attempt = LessonAttemptEntity.Start(lesson.Id, "student-alice", 1, 10);
        var answer = attempt.SubmitAnswer(exercise.Id, null, 1, GradingMode.AiRubric, 10, "{\"transcribedText\":\"I went to Paris.\"}", SubmissionGradingStatus.Pending);
        attempt.ApplyGradingResult(answer.Id, SubmissionGradingStatus.Completed, 80m, 8.0m, "{}", clock.GetUtcNow());

        db.Lessons.Add(lesson);
        db.LessonAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var bob = new FakeCurrentUser { UserId = "student-bob" };
        var deterministicGrader = new DeterministicExerciseGrader(new DictationScorer());
        var aiGrader = new FakeLessonAiGrader();
        var retryHandler = new RetryAnswerGradingCommandHandler(db, bob, clock, deterministicGrader, aiGrader, uow, idempotency);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            retryHandler.HandleAsync(new RetryAnswerGradingCommand(attempt.Id, answer.Id, attempt.AttemptRevision)));
    }

    // =========================================================================
    // 5. Durable Idempotency Tests
    // =========================================================================

    [Fact]
    public async Task Idempotency_SameKeyAndFingerprint_ReturnsReplayResultWithoutReExecuting()
    {
        var (db, clock, user, _, idempotency) = CreateContext("learner-alice");

        string opKey = "op-test-key-001";
        string fingerprint = "canonical-fingerprint-abc";

        // Step 1: CheckOrBeginOperation -> returns null (new operation)
        var check1 = await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: opKey,
            requestFingerprint: fingerprint);

        Assert.Null(check1);

        // Step 2: Record successful result
        var cachedPayload = new ExerciseAnswerResultDto(
            AnswerId: Guid.NewGuid(),
            AttemptId: Guid.NewGuid(),
            AttemptRevision: 2,
            ExerciseId: Guid.NewGuid(),
            SubQuestionId: null,
            Status: "Completed",
            GradingMode: "Deterministic",
            Score100: 95m,
            PointsEarned: 9.5m,
            MaxPoints: 10m,
            RubricCode: null,
            RubricVersion: null,
            CriteriaBreakdown: null,
            Strengths: null,
            Improvements: null,
            SuggestedAnswer: "Well done",
            AttemptProgress: new AttemptProgressDto(1, 1, 0, 0, 9.5m, 10m, 95m));

        await idempotency.CompleteOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: opKey,
            statusCode: 200,
            responsePayload: cachedPayload);

        // Step 3: CheckOrBeginOperation with same key and same fingerprint -> returns replay result
        var check2 = await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: opKey,
            requestFingerprint: fingerprint);

        Assert.NotNull(check2);
        Assert.True(check2.IsReplay);
        var replayedDto = JsonSerializer.Deserialize<ExerciseAnswerResultDto>(check2.ResponsePayloadJson!);
        Assert.NotNull(replayedDto);
        Assert.Equal(95m, replayedDto.Score100);
        Assert.Equal(2, replayedDto.AttemptRevision);
    }

    [Fact]
    public async Task Idempotency_SameKeyDifferentFingerprint_ThrowsConflictException()
    {
        var (db, clock, user, _, idempotency) = CreateContext("learner-alice");

        string opKey = "op-test-key-conflict";

        // Begin operation with fingerprint 1
        await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: opKey,
            requestFingerprint: "fingerprint-alpha");

        // Same operation key, but different payload fingerprint -> must return 409 Conflict result
        var conflictResult = await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: opKey,
            requestFingerprint: "fingerprint-beta-different-payload");

        Assert.NotNull(conflictResult);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Contains("IDEMPOTENCY_KEY_PAYLOAD_MISMATCH", conflictResult.ErrorMessage);
    }

    [Fact]
    public async Task Idempotency_DifferentUsersWithSameKey_DoNotCollide()
    {
        var (db, clock, user, _, idempotency) = CreateContext("learner-alice");

        string sharedKey = "shared-op-key";

        // Alice begins operation
        var aliceCheck = await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-alice",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: sharedKey,
            requestFingerprint: "fingerprint-1");
        Assert.Null(aliceCheck);

        // Bob begins operation with same key and same resource scope -> should succeed independently
        var bobCheck = await idempotency.CheckOrBeginOperationAsync(
            userId: "learner-bob",
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: "attempt-123",
            operationKey: sharedKey,
            requestFingerprint: "fingerprint-2");
        Assert.Null(bobCheck);
    }

    // =========================================================================
    // 6. HTTP Resilience & Security Tests
    // =========================================================================

    [Fact]
    public async Task GeminiHttpHelper_RetriesOnTransient503_AndSucceeds()
    {
        int callCount = 0;
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"success response\"}]}}]}", Encoding.UTF8, "application/json")
            });
        });

        using var client = new HttpClient(mockHandler);

        var (success, statusCode, body) = await GeminiHttpHelper.PostJsonWithRetryAsync(
            client,
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
            new { prompt = "hello" },
            apiKey: "test-api-key",
            maxRetries: 2,
            logger: NullLogger.Instance,
            ct: CancellationToken.None);

        Assert.True(success);
        Assert.Equal(HttpStatusCode.OK, statusCode);
        Assert.Equal(2, callCount); // 1 retry made
    }

    [Fact]
    public async Task GeminiHttpHelper_DoesNotRetryOn400Or401()
    {
        int callCount = 0;
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":{\"message\":\"API key not valid\"}}", Encoding.UTF8, "application/json")
            });
        });

        using var client = new HttpClient(mockHandler);

        var (success, statusCode, body) = await GeminiHttpHelper.PostJsonWithRetryAsync(
            client,
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
            new { prompt = "hello" },
            apiKey: "invalid-key",
            maxRetries: 3,
            logger: NullLogger.Instance,
            ct: CancellationToken.None);

        Assert.False(success);
        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
        Assert.Equal(1, callCount); // No retry for 401 Unauthorized
    }

    [Fact]
    public async Task GeminiHttpHelper_UsesHeaderForApiKey_DoesNotExposeKeyInUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[]}", Encoding.UTF8, "application/json")
            });
        });

        using var client = new HttpClient(mockHandler);

        await GeminiHttpHelper.PostJsonWithRetryAsync(
            client,
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
            new { },
            apiKey: "SECRET_KEY_12345",
            maxRetries: 1,
            logger: NullLogger.Instance,
            ct: CancellationToken.None);

        Assert.NotNull(capturedRequest);
        // Header x-goog-api-key must contain the key
        Assert.True(capturedRequest.Headers.Contains("x-goog-api-key"));
        Assert.Equal("SECRET_KEY_12345", capturedRequest.Headers.GetValues("x-goog-api-key").First());
        // URL must NOT contain key parameter
        Assert.DoesNotContain("key=", capturedRequest.RequestUri?.Query ?? string.Empty);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func)
        {
            _func = func;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _func(request, cancellationToken);
        }
    }
}
