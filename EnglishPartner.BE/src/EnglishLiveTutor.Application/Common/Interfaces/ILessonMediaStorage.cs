namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ILessonMediaStorage
{
    Task<string> SaveAsync(Stream stream, string extension, string contentType, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
