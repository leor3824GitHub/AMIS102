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
    ReturnedPropertyReceipt = 0,

    /// <summary>An Inventory Custodian Slip (ICS) or Property Acknowledgement Receipt (PAR), keyed by its
    /// <c>PropertyAccountability</c> Id. Uploadable once the receiving employee has accepted it (the
    /// document is then legally in force).</summary>
    PropertyAccountability = 1,

    /// <summary>A Property Issuance / transfer report — PPEIR (PPE) or SMIR (semi-expendable), keyed by its
    /// <c>PropertyIssuanceReport</c> Id. Created atomically (the act of creating it issues the assets), so it
    /// is a document of record the moment it exists.</summary>
    IssuanceReport = 2,

    /// <summary>A Receiving Report — PPERR (PPE) or SMRR (semi-expendable), keyed by its <c>ReceivingReport</c>
    /// Id. Created atomically, so it is a document of record the moment it exists.</summary>
    ReceivingReport = 3,

    /// <summary>An Inventory and Inspection Report of Unserviceable Property — IIRUP (PPE) or IIRUSP
    /// (semi-expendable), keyed by its <c>UnserviceablePropertyReport</c> Id. Uploadable once disposal has
    /// been recorded or the report is closed.</summary>
    UnserviceableReport = 4,

    /// <summary>A Report of Lost, Stolen, Damaged or Destroyed Property (RLSDDSP), keyed by its
    /// <c>PropertyIncidentReport</c> Id. Uploadable once the incident is resolved or closed.</summary>
    IncidentReport = 5,

    /// <summary>A Report on the Physical Count of Property — RPCPPE (PPE) or RPCSE (semi-expendable), keyed by
    /// its <c>PhysicalCountSession</c> Id. Uploadable once the count has been closed (signed off).</summary>
    PhysicalCountReport = 6
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
