namespace AMIS.Framework.Core.Domain;

/// <summary>
/// The uploaded wet-signed PDF copy of a document of record, modeled as an owned value object on the
/// document aggregate that carries it (see <see cref="ISignedCopyHolder"/>). The PDF bytes live in object
/// storage; this value holds the storage key plus a SHA-256 integrity hash and upload audit. One current
/// copy per document — re-uploading replaces the value.
/// </summary>
/// <remarks>
/// Signed copies are PDF-only (enforced by the upload validators via a <c>.pdf</c> extension rule and a
/// <c>%PDF</c> magic-byte check), so the content type is the constant <c>"application/pdf"</c> and is not
/// stored. The acting uploader's identity is captured by the owning aggregate's audit fields at upload time;
/// only the display <see cref="UploadedByName"/> (which may be a resolved signatory name) is stored here.
/// </remarks>
public sealed record SignedCopy(
    string StorageKey,
    string Sha256,
    string FileName,
    long FileSizeBytes,
    string? UploadedByName,
    DateTimeOffset UploadedOnUtc);
