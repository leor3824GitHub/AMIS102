using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage.DTOs;

namespace AMIS.Framework.Storage.Services;

public interface IStorageService
{
    /// <summary>
    /// Stores <paramref name="request"/> and returns its storage key. When <paramref name="folderPath"/>
    /// is supplied it determines the sub-folder under the upload root (segments are sanitized); otherwise
    /// the storage entity type name <typeparamref name="T"/> is used. Pass
    /// <see cref="StoragePaths.Protected(string?, string)"/> for confidential files that must not be
    /// served as anonymous static content.
    /// </summary>
    Task<string> UploadAsync<T>(
        FileUploadRequest request,
        FileType fileType,
        string? folderPath = null,
        CancellationToken cancellationToken = default) where T : class;

    Task<FileDownloadResponse?> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string path, CancellationToken cancellationToken = default);
}
