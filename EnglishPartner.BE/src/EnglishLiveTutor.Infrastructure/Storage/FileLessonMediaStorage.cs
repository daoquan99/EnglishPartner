using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Storage;

public sealed class FileLessonMediaStorage : ILessonMediaStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webm", ".wav", ".mp3", ".ogg", ".m4a", ".aac"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/webm", "audio/wav", "audio/x-wav", "audio/mpeg", "audio/mp3",
        "audio/ogg", "application/ogg", "audio/m4a", "audio/x-m4a", "audio/mp4",
        "audio/aac", "application/octet-stream"
    };

    private readonly string _storageRoot;
    private readonly long _maxSizeBytes;
    private readonly ILogger<FileLessonMediaStorage> _logger;

    public FileLessonMediaStorage(
        IOptions<LessonMediaStorageOptions> options,
        ILogger<FileLessonMediaStorage> logger)
    {
        _logger = logger;
        _maxSizeBytes = options.Value.MaxFileSizeBytes > 0 ? options.Value.MaxFileSizeBytes : 25 * 1024 * 1024;

        string root = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "storage", "lesson-media");
        }

        _storageRoot = Path.GetFullPath(root);
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveAsync(Stream stream, string extension, string contentType, CancellationToken ct = default)
    {
        if (stream == null || !stream.CanRead)
            throw new ArgumentException("Stream is null or cannot be read.", nameof(stream));

        string cleanExt = string.IsNullOrWhiteSpace(extension) ? ".webm" : extension.Trim().ToLowerInvariant();
        if (!cleanExt.StartsWith('.')) cleanExt = "." + cleanExt;

        if (!AllowedExtensions.Contains(cleanExt))
        {
            throw new ArgumentException($"Unsupported audio extension '{cleanExt}'. Allowed extensions: {string.Join(", ", AllowedExtensions)}");
        }

        string cleanMime = (contentType ?? "audio/webm").Split(';')[0].Trim().ToLowerInvariant();
        if (!AllowedMimeTypes.Contains(cleanMime))
        {
            throw new ArgumentException($"Unsupported audio MIME type '{cleanMime}'.");
        }

        // Server-generated opaque key: media/{year}/{month}/{Guid}_{Guid}{ext}
        var now = DateTime.UtcNow;
        string subDir = Path.Combine(_storageRoot, now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(subDir);

        string fileName = $"{Guid.NewGuid():N}_{Guid.NewGuid():N}{cleanExt}";
        string physicalPath = Path.Combine(subDir, fileName);
        string storageKey = $"media/{now:yyyy}/{now:MM}/{fileName}";

        long bytesWritten = 0;
        await using (var fileStream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            byte[] buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                bytesWritten += read;
                if (bytesWritten > _maxSizeBytes)
                {
                    fileStream.Close();
                    File.Delete(physicalPath);
                    throw new InvalidOperationException($"File exceeds maximum allowed size of {_maxSizeBytes / (1024 * 1024)}MB.");
                }
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }

        if (bytesWritten == 0)
        {
            File.Delete(physicalPath);
            throw new ArgumentException("Uploaded media file contains 0 bytes.");
        }

        _logger.LogInformation("Saved media file to {StorageKey} ({Bytes} bytes)", storageKey, bytesWritten);
        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        string? physicalPath = ResolvePhysicalPath(storageKey);
        if (physicalPath == null || !File.Exists(physicalPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(fs);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        string? physicalPath = ResolvePhysicalPath(storageKey);
        return Task.FromResult(physicalPath != null && File.Exists(physicalPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        string? physicalPath = ResolvePhysicalPath(storageKey);
        if (physicalPath != null && File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Deleted media file {StorageKey}", storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical media file {PhysicalPath}", physicalPath);
            }
        }
        return Task.CompletedTask;
    }

    private string? ResolvePhysicalPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;

        // Strip prefix "media/" if present
        string relative = storageKey.Trim().Replace('\\', '/');
        if (relative.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
        {
            relative = relative["media/".Length..];
        }

        // Prevent path traversal
        string normalized = relative.Replace("..", "").Replace("//", "/");
        string full = Path.GetFullPath(Path.Combine(_storageRoot, normalized));

        if (!full.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null; // Path traversal detected
        }

        return full;
    }
}
