using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Api.Services;

/// <summary>
/// Local filesystem storage implementation.
/// Stores files at {BasePath}/{folder}/{uniqueId}_{fileName}.
/// </summary>
internal sealed class LocalFileStorageService(IOptions<FileStorageSettings> options) : IFileStorageService
{
    private readonly FileStorageSettings _settings = options.Value;

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_settings.BasePath, folder);
        Directory.CreateDirectory(directory);

        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(directory, uniqueFileName);

        await using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return $"{_settings.BaseUrl}/{folder}/{uniqueFileName}";
    }

    public Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string relativePath) => relativePath;

    private string ResolveFullPath(string relativePath)
    {
        var cleaned = relativePath
            .TrimStart('/')
            .Replace(_settings.BaseUrl.TrimStart('/'), string.Empty)
            .TrimStart('/');
        return Path.Combine(_settings.BasePath, cleaned);
    }
}
