namespace EnglishPartner.SharedKernel.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    string GetPublicUrl(string relativePath);
}
