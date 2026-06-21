using Mediator;

namespace AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>The Budget Disbursement documents that support an uploaded wet-signed official copy.</summary>
public enum BudgetDisbursementDocumentType
{
    DisbursementVoucher = 0,
    BudgetUtilizationRequest = 1
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Metadata about the wet-signed copy stored in object storage (no file bytes).</summary>
public sealed record SignedDocumentDto(
    BudgetDisbursementDocumentType DocumentType,
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
/// Uploads the scanned wet-signed copy of a Budget Disbursement document of record. The document must be in its
/// terminal signed state (DV Approved or Paid). Stored in object storage with a SHA-256 integrity hash;
/// re-uploading replaces the current copy.
/// </summary>
public sealed record UploadSignedDocumentCommand(
    BudgetDisbursementDocumentType DocumentType,
    Guid DocumentId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<SignedDocumentDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Returns metadata for the signed copy of a document, or null if none uploaded yet.</summary>
public sealed record GetSignedDocumentQuery(BudgetDisbursementDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentDto?>;

/// <summary>Downloads the stored signed copy, verifying its SHA-256 hash before returning it.</summary>
public sealed record DownloadSignedDocumentQuery(BudgetDisbursementDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentFileDto?>;