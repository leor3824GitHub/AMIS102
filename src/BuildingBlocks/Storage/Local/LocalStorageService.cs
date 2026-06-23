using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage.DTOs;
using AMIS.Framework.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;

namespace AMIS.Framework.Storage.Local;

public class LocalStorageService : IStorageService
{
    private readonly string _rootPath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public LocalStorageService(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _rootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    public async Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, string? folderPath = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }

        if (request.Data.Count > rules.MaxSizeInMB * 1024 * 1024)
        {
            throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMB} MB.");
        }

        var folder = StoragePaths.ResolveFolder(folderPath, typeof(T).Name);
        var safeFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(request.FileName)}";
        var relativePath = $"{StoragePaths.UploadBasePath}/{folder}/{safeFileName}"; // forward-slash key (URL-normalized)
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, request.Data.ToArray(), cancellationToken);

        return relativePath;
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult<FileDownloadResponse?>(null);
        }

        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileDownloadResponse?>(null);
        }

        var fileInfo = new FileInfo(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);

        return Task.FromResult<FileDownloadResponse?>(new FileDownloadResponse
        {
            Stream = stream,
            ContentType = contentType,
            FileName = fileName,
            ContentLength = fileInfo.Length
        });
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        return Task.FromResult(File.Exists(fullPath));
    }

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return Task.CompletedTask;

        var fullPath = Path.Combine(_rootPath, path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string fileName)
    {
        return Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
    }
}

