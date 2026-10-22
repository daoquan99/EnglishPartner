using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

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
    private readonly IAudioRecordingRepository _audioRepository;
    private readonly ICurrentUser _currentUser;

    public GetLessonMediaQueryHandler(IAudioRecordingRepository audioRepository, ICurrentUser currentUser)
    {
        _audioRepository = audioRepository;
        _currentUser = currentUser;
    }

    public async Task<LessonMediaDto> HandleAsync(GetLessonMediaQuery query, CancellationToken ct = default)
    {
        var recording = await _audioRepository.GetByIdAsync(query.Id, ct);
        if (recording == null || recording.IsDeleted)
        {
            throw new NotFoundException("Media asset not found.");
        }

        string? currentUserId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(currentUserId) ||
            !string.Equals(recording.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
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
