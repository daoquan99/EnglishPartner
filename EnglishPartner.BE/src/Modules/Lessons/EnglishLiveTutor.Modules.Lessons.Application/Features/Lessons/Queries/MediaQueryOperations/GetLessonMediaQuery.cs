using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.MediaQueryOperations;

public sealed record LessonMediaDto(
    string AssetId,
    string ContentType,
    long FileSizeBytes,
    string StorageKey,
    string StreamUrl);

public sealed record GetLessonMediaQuery(Guid Id) : IQuery<LessonMediaDto>;

public sealed class GetLessonMediaQueryHandler : IQueryHandler<GetLessonMediaQuery, LessonMediaDto>
{
    private readonly IAudioRecordingStore _audioStore;
    private readonly ICurrentUser _currentUser;

    public GetLessonMediaQueryHandler(IAudioRecordingStore audioStore, ICurrentUser currentUser)
    {
        _audioStore = audioStore;
        _currentUser = currentUser;
    }

    public async Task<LessonMediaDto> HandleAsync(GetLessonMediaQuery query, CancellationToken ct = default)
    {
        string? currentUserId = _currentUser.UserId;
        var recording = string.IsNullOrWhiteSpace(currentUserId)
            ? null
            : await _audioStore.GetOwnedAsync(query.Id, currentUserId, ct);
        if (recording == null || recording.IsDeleted)
        {
            // Do not leak existence of another user's asset
            throw new NotFoundException("Media asset not found.");
        }

        return new LessonMediaDto(
            AssetId: recording.Id.ToString(),
            ContentType: recording.ContentType,
            FileSizeBytes: recording.FileSizeBytes,
            StorageKey: recording.StorageKey,
            StreamUrl: $"/api/lesson-media/{recording.Id}/stream");
    }
}
