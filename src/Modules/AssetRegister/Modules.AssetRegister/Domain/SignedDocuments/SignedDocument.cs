using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;

namespace AMIS.Modules.AssetRegister.Domain.SignedDocuments;

/// <summary>
/// The uploaded scanned wet-signed copy of an Asset Register document of record (RRSP/RRP). The file lives
/// in object storage; this row holds the storage key plus a SHA-256 integrity hash and upload audit.
/// One current copy per (tenant, document) — re-uploading <see cref="Replace"/>s it.
/// </summary>
public sealed class SignedDocument : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public AssetRegisterDocumentType DocumentType { get; private set; }
    public Guid DocumentId { get; private set; }

    public string StorageKey { get; private set; } = default!;
    public string Sha256 { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }

    public Guid? UploadedById { get; private set; }
    public string? UploadedByName { get; private set; }
    public DateTimeOffset UploadedOnUtc { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private SignedDocument() { }

    public static SignedDocument Create(
        string tenantId,
        AssetRegisterDocumentType documentType,
        Guid documentId,
        string storageKey,
        string sha256,
        string fileName,
        string contentType,
        long fileSizeBytes,
        Guid? uploadedById,
        string? uploadedByName)
    {
        return new SignedDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentType = documentType,
            DocumentId = documentId,
            StorageKey = storageKey,
            Sha256 = sha256,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedById = uploadedById,
            UploadedByName = uploadedByName,
            UploadedOnUtc = DateTimeOffset.UtcNow,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Replaces the stored file (new key/hash/metadata) when a corrected scan is uploaded.</summary>
    public void Replace(
        string storageKey,
        string sha256,
        string fileName,
        string contentType,
        long fileSizeBytes,
        Guid? uploadedById,
        string? uploadedByName)
    {
        StorageKey = storageKey;
        Sha256 = sha256;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedById = uploadedById;
        UploadedByName = uploadedByName;
        UploadedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = UploadedOnUtc;
    }
}
