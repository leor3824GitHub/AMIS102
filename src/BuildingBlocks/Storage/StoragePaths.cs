using System.Text.RegularExpressions;

namespace AMIS.Framework.Storage;

/// <summary>
/// Well-known storage path conventions shared by the storage providers (<c>LocalStorageService</c>,
/// <c>S3StorageService</c>) and the upload handlers that place confidential files.
/// </summary>
/// <remarks>
/// The web pipeline blocks the matching URL prefix from anonymous static serving
/// (see <c>AMIS.Framework.Web.AMISPipelineOptions.BlockedStaticPathPrefixes</c>, which defaults to
/// <c>/uploads/protected</c>). Keep the two in sync: anything written under <see cref="ProtectedFolder"/>
/// is reachable only through an authenticated, permission-checked download endpoint.
/// </remarks>
public static class StoragePaths
{
    /// <summary>Base upload folder under the static web root / bucket. Every provider key begins with this.</summary>
    public const string UploadBasePath = "uploads";

    /// <summary>
    /// Sub-folder (under <see cref="UploadBasePath"/>) for confidential documents — e.g. wet-signed
    /// copies — that must never be served as anonymous static files. Build the upload <c>folderPath</c>
    /// for such a document with <see cref="Protected(string?, string)"/>.
    /// </summary>
    public const string ProtectedFolder = "protected";

    /// <summary>
    /// Builds the confidential upload folder for a tenant + logical document type, e.g.
    /// <c>protected/root/receivingreport</c>. The provider prepends <see cref="UploadBasePath"/> and
    /// sanitizes each path segment, so the resulting key is <c>uploads/protected/{tenant}/{docType}/{guid}_{file}</c>.
    /// </summary>
    public static string Protected(string? tenantId, string documentType) =>
        $"{ProtectedFolder}/{tenantId}/{documentType}";

    /// <summary>
    /// Resolves a caller-supplied <paramref name="folderPath"/> into a safe, forward-slash folder under
    /// <see cref="UploadBasePath"/>. Each segment is lower-cased and stripped to <c>[a-z0-9_]</c>, which
    /// neutralizes path-traversal (<c>..</c>, drive letters, separators). When no folder is supplied the
    /// storage entity's type name is used (preserving the historical per-type layout).
    /// </summary>
    internal static string ResolveFolder(string? folderPath, string fallbackTypeName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return SanitizeSegment(fallbackTypeName);
        }

        var segments = folderPath
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeSegment)
            .Where(s => s.Length > 0);

        var joined = string.Join('/', segments);
        return joined.Length > 0 ? joined : SanitizeSegment(fallbackTypeName);
    }

    private static string SanitizeSegment(string segment)
    {
#pragma warning disable CA1308 // folder names are intentionally lower-case for URLs/paths
        return Regex.Replace(segment.ToLowerInvariant(), "[^a-z0-9]", "_");
#pragma warning restore CA1308
    }
}
