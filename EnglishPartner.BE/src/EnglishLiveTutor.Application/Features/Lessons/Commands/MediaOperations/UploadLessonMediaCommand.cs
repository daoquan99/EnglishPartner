using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Validation;
using FluentValidation.Results;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.MediaOperations;

public sealed record UploadLessonMediaResultDto(
    string AssetId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Url);

public sealed record UploadLessonMediaCommand(
    Stream Stream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? OperationKey = null) : ICommand<UploadLessonMediaResultDto>;

public sealed class UploadLessonMediaCommandHandler : ICommandHandler<UploadLessonMediaCommand, UploadLessonMediaResultDto>
{
    private readonly IAudioRecordingRepository _audioRepository;
    private readonly ILessonMediaStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UploadLessonMediaCommandHandler(
        IAudioRecordingRepository audioRepository,
        ILessonMediaStorage storage,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _audioRepository = audioRepository;
        _storage = storage;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<UploadLessonMediaResultDto> HandleAsync(UploadLessonMediaCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to upload media.");
        }

        string rawFileName = Path.GetFileName(command.FileName);
        string sanitizedFileName = string.IsNullOrWhiteSpace(rawFileName) ? "audio.webm" : rawFileName;
        string extension = Path.GetExtension(sanitizedFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".webm";
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".webm", ".ogg", ".m4a", ".aac"
        };
        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "audio/mpeg", "audio/mp3", "audio/wav", "audio/x-wav", "audio/webm", "audio/ogg", "audio/m4a", "audio/aac", "audio/mp4", "application/octet-stream"
        };

        if (command.FileSizeBytes <= 0)
        {
            throw new ValidationException(new[] { new ValidationFailure("File", "File cannot be empty.") });
        }

        if (command.FileSizeBytes > 50 * 1024 * 1024)
        {
            throw new ValidationException(new[] { new ValidationFailure("File", "File size exceeds the 50MB limit.") });
        }

        if (!allowedExtensions.Contains(extension) || (!string.IsNullOrWhiteSpace(command.ContentType) && !allowedMimeTypes.Contains(command.ContentType)))
        {
            throw new ValidationException(new[] { new ValidationFailure("File", $"Unsupported audio format: '{extension}' or MIME type: '{command.ContentType}'.") });
        }

        // 1. Save stream to physical media storage
        string storageKey = await _storage.SaveAsync(command.Stream, extension, command.ContentType, ct);

        // 2. Persist AudioRecordingEntity in DB
        var assetId = Guid.NewGuid();
        var recording = new AudioRecordingEntity(
            userId: userId,
            storageKey: storageKey,
            contentType: command.ContentType,
            fileSizeBytes: command.FileSizeBytes,
            durationMilliseconds: 0,
            id: assetId);

        try
        {
            _audioRepository.Add(recording);
            await _unitOfWork.CommitChangesAsync(ct);
        }
        catch
        {
            // Clean up saved file if DB commit fails
            await _storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }

        return new UploadLessonMediaResultDto(
            AssetId: assetId.ToString(),
            FileName: sanitizedFileName,
            ContentType: command.ContentType,
            FileSizeBytes: command.FileSizeBytes,
            Url: $"/api/lesson-media/{assetId}");
    }
}
