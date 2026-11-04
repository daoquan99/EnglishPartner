using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.MediaQueryOperations;

public sealed record LessonMediaStreamDto(
    Stream Stream,
    string ContentType,
    string FileName,
    long FileSizeBytes);

public sealed record GetLessonMediaStreamQuery(Guid Id) : IQuery<LessonMediaStreamDto>;

public sealed class GetLessonMediaStreamQueryHandler : IQueryHandler<GetLessonMediaStreamQuery, LessonMediaStreamDto>
{
    private readonly IAudioRecordingStore _audioStore;
    private readonly ILessonMediaStorage _storage;
    private readonly ICurrentUser _currentUser;

    public GetLessonMediaStreamQueryHandler(
        IAudioRecordingStore audioStore,
        ILessonMediaStorage storage,
        ICurrentUser currentUser)
    {
        _audioStore = audioStore;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<LessonMediaStreamDto> HandleAsync(GetLessonMediaStreamQuery query, CancellationToken ct = default)
    {
        string? currentUserId = _currentUser.UserId;
        var recording = string.IsNullOrWhiteSpace(currentUserId)
            ? null
            : await _audioStore.GetOwnedAsync(query.Id, currentUserId, ct);
        if (recording == null || recording.IsDeleted)
        {
            throw new NotFoundException("Media asset not found.");
        }

        var stream = await _storage.OpenReadAsync(recording.StorageKey, ct);
        if (stream == null)
        {
            throw new NotFoundException("Media file content not found in storage.");
        }

        string fileName = Path.GetFileName(recording.StorageKey);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"{recording.Id}.webm";
        }

        return new LessonMediaStreamDto(
            Stream: stream,
            ContentType: recording.ContentType,
            FileName: fileName,
            FileSizeBytes: recording.FileSizeBytes);
    }
}
