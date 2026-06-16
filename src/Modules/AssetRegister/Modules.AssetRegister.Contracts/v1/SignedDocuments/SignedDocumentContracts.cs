using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>The Asset Register documents that support an uploaded wet-signed official copy.</summary>
public enum AssetRegisterDocumentType
{
    /// <summary>A Receipt for Returned (Semi-Expendable) Property — RRSP/RRP, keyed by its receipt Id.
    /// Uploadable only once the return has been Accepted (the receipt number is assigned).</summary>
    ReturnedPropertyReceipt = 0
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Metadata about the wet-signed copy stored in object storage (no file bytes).</summary>
public sealed record SignedDocumentDto(
    AssetRegisterDocumentType DocumentType,
    Guid DocumentId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256,
    string? UploadedByName,
    DateTimeOffset UploadedOnUtc);

/// <summary>The verified file bytes of a stored signed copy, returned for download.</summary>
public sealed record SignedDocumentFileDto(
    byte[] Content,
    string ContentType,
    string FileName);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Uploads the scanned wet-signed copy of an Asset Register document of record. The document must be in
/// its terminal signed state (RRSP/RRP Accepted). Stored in object storage with a SHA-256 integrity hash;
/// re-uploading replaces the current copy.
/// </summary>
public sealed record UploadSignedDocumentCommand(
    AssetRegisterDocumentType DocumentType,
    Guid DocumentId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<SignedDocumentDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Returns metadata for the signed copy of a document, or null if none uploaded yet.</summary>
public sealed record GetSignedDocumentQuery(AssetRegisterDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentDto?>;

/// <summary>Downloads the stored signed copy, verifying its SHA-256 hash before returning it.</summary>
public sealed record DownloadSignedDocumentQuery(AssetRegisterDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentFileDto?>;
