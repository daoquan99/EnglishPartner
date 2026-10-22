using System.IO;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Domain.Tests;

public class FakeLessonAiGrader : ILessonAiGrader
{
    public LessonAiGradingOutcome OutcomeToReturn { get; set; } = new(
        IsAssessable: true,
        Score100: 80m,
        PointsEarned: 4.0m,
        CriteriaBreakdown: new List<LessonAiCriterionScore>
        {
            new("ACC", "Accuracy", 1.0m, 3, "Good accuracy", "Minor errors")
        },
        Strengths: new List<string> { "Clear structure" },
        Improvements: new List<string> { "Expand vocabulary" },
        SuggestedAnswer: "Sample answer");

    public int CallCount { get; private set; }

    public Task<LessonAiGradingOutcome> GradeSubmissionAsync(
        ExerciseType exerciseType,
        string taskInstructions,
        string cefrLevel,
        ScoringRubricEntity rubric,
        string learnerSubmissionPayloadJson,
        decimal maxPoints,
        CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(OutcomeToReturn);
    }
}

public class FakeAudioRecordingRepository : IAudioRecordingRepository
{
    private readonly Dictionary<Guid, AudioRecordingEntity> _recordings = new();

    public Task<AudioRecordingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _recordings.TryGetValue(id, out var rec);
        return Task.FromResult(rec);
    }

    public void Add(AudioRecordingEntity recording)
    {
        _recordings[recording.Id] = recording;
    }
}

public class FakeLessonMediaStorage : ILessonMediaStorage
{
    private readonly Dictionary<string, byte[]> _files = new();

    public Task<string> SaveAsync(Stream stream, string extension, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var key = $"media/{Guid.NewGuid()}{extension}";
        _files[key] = ms.ToArray();
        return Task.FromResult(key);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        if (_files.TryGetValue(storageKey, out var bytes))
        {
            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        }
        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        return Task.FromResult(_files.ContainsKey(storageKey));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        _files.Remove(storageKey);
        return Task.CompletedTask;
    }
}

public class FakeRolePlayReplyProvider : IRolePlayReplyProvider
{
    public Func<string, string, IReadOnlyList<RolePlayHistoryItem>, int, int, string>? ReplyFunc { get; set; }

    public Task<string> GenerateReplyAsync(
        string scenario,
        string aiRole,
        IReadOnlyList<RolePlayHistoryItem> conversationHistory,
        int currentTurn,
        int maxTurns,
        CancellationToken ct = default)
    {
        if (ReplyFunc != null)
        {
            return Task.FromResult(ReplyFunc(scenario, aiRole, conversationHistory, currentTurn, maxTurns));
        }
        return Task.FromResult($"AI response as {aiRole} for turn {currentTurn}");
    }
}
