using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Domain.SignedDocuments;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Domain;

public sealed class SignedDocumentDomainTests
{
    [Fact]
    public void Create_PopulatesMetadataAndUploadTimestamp()
    {
        var docId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var sd = SignedDocument.Create(
            tenantId: "root",
            documentType: ProcurementDocumentType.PurchaseRequest,
            documentId: docId,
            storageKey: "uploads/signed_document/abc_PR.pdf",
            sha256: new string('a', 64),
            fileName: "PR-2026-0001-signed.pdf",
            contentType: "application/pdf",
            fileSizeBytes: 12345,
            uploadedById: uploaderId,
            uploadedByName: "Roel Caperig");

        sd.Id.ShouldNotBe(Guid.Empty);
        sd.DocumentType.ShouldBe(ProcurementDocumentType.PurchaseRequest);
        sd.DocumentId.ShouldBe(docId);
        sd.StorageKey.ShouldBe("uploads/signed_document/abc_PR.pdf");
        sd.Sha256.ShouldBe(new string('a', 64));
        sd.FileSizeBytes.ShouldBe(12345);
        sd.UploadedById.ShouldBe(uploaderId);
        sd.UploadedByName.ShouldBe("Roel Caperig");
        sd.UploadedOnUtc.ShouldNotBe(default);
    }

    [Fact]
    public void Replace_SwapsFileMetadataAndStampsModified()
    {
        var sd = SignedDocument.Create(
            "root", ProcurementDocumentType.PurchaseOrder, Guid.NewGuid(),
            "uploads/signed_document/old.pdf", new string('a', 64), "old.pdf", "application/pdf", 100, Guid.NewGuid(), "Old User");

        sd.Replace(
            storageKey: "uploads/signed_document/new.pdf",
            sha256: new string('b', 64),
            fileName: "new.pdf",
            contentType: "application/pdf",
            fileSizeBytes: 200,
            uploadedById: Guid.NewGuid(),
            uploadedByName: "New User");

        sd.StorageKey.ShouldBe("uploads/signed_document/new.pdf");
        sd.Sha256.ShouldBe(new string('b', 64));
        sd.FileName.ShouldBe("new.pdf");
        sd.FileSizeBytes.ShouldBe(200);
        sd.UploadedByName.ShouldBe("New User");
        sd.LastModifiedOnUtc.ShouldNotBeNull();
    }
}
