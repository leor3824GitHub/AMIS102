using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>The procurement documents that support an uploaded wet-signed official copy.</summary>
public enum ProcurementDocumentType
{
    PurchaseRequest = 0,
    PurchaseOrder = 1,
    AbstractOfCanvass = 2,
    InspectionAcceptanceReport = 3,

    /// <summary>A single supplier's wet-signed Request for Quotation, keyed by its <c>CanvassQuotation</c> Id
    /// (up to one per quotation, so up to 3 per canvass). Unlike the Abstract of Canvass, the source document
    /// exists as soon as the quotation is recorded, so it may be uploaded at any non-cancelled canvass stage.</summary>
    RequestForQuotation = 4
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Metadata about the wet-signed copy stored in object storage (no file bytes).</summary>
public sealed record SignedDocumentDto(
    ProcurementDocumentType DocumentType,
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
/// Uploads the scanned wet-signed copy of a procurement document of record. The document must be in
/// its terminal signed state (PR Approved, PO Issued, Canvass Awarded). Stored in object storage with
/// a SHA-256 integrity hash; re-uploading replaces the current copy.
/// </summary>
public sealed record UploadSignedDocumentCommand(
    ProcurementDocumentType DocumentType,
    Guid DocumentId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<SignedDocumentDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Returns metadata for the signed copy of a document, or null if none uploaded yet.</summary>
public sealed record GetSignedDocumentQuery(ProcurementDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentDto?>;

/// <summary>Downloads the stored signed copy, verifying its SHA-256 hash before returning it.</summary>
public sealed record DownloadSignedDocumentQuery(ProcurementDocumentType DocumentType, Guid DocumentId)
    : IQuery<SignedDocumentFileDto?>;
