using AMIS.Framework.Core.Domain;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Domain;

public sealed class SignedCopyDomainTests
{
    [Fact]
    public void Create_PopulatesMetadataAndUploadTimestamp()
    {
        var uploadedOn = DateTimeOffset.UtcNow;

        var copy = new SignedCopy(
            StorageKey: "uploads/protected/root/purchaserequest/abc_PR.pdf",
            Sha256: new string('a', 64),
            FileName: "PR-2026-0001-signed.pdf",
            FileSizeBytes: 12345,
            UploadedByName: "Roel Caperig",
            UploadedOnUtc: uploadedOn);

        copy.StorageKey.ShouldBe("uploads/protected/root/purchaserequest/abc_PR.pdf");
        copy.Sha256.ShouldBe(new string('a', 64));
        copy.FileName.ShouldBe("PR-2026-0001-signed.pdf");
        copy.FileSizeBytes.ShouldBe(12345);
        copy.UploadedByName.ShouldBe("Roel Caperig");
        copy.UploadedOnUtc.ShouldBe(uploadedOn);
    }

    [Fact]
    public void With_ReplacesFileMetadataAndLeavesOriginalUnchanged()
    {
        // A re-upload replaces the aggregate's SignedCopy value (SetSignedCopy assigns a new instance).
        var original = new SignedCopy(
            "uploads/old.pdf", new string('a', 64), "old.pdf", 100, "Old User", DateTimeOffset.UtcNow);

        var replaced = original with
        {
            StorageKey = "uploads/new.pdf",
            Sha256 = new string('b', 64),
            FileName = "new.pdf",
            FileSizeBytes = 200,
            UploadedByName = "New User",
            UploadedOnUtc = DateTimeOffset.UtcNow,
        };

        replaced.StorageKey.ShouldBe("uploads/new.pdf");
        replaced.Sha256.ShouldBe(new string('b', 64));
        replaced.FileName.ShouldBe("new.pdf");
        replaced.FileSizeBytes.ShouldBe(200);
        replaced.UploadedByName.ShouldBe("New User");

        // Value object is immutable — the original is untouched, and the two are not equal.
        replaced.ShouldNotBe(original);
        original.FileName.ShouldBe("old.pdf");
    }
}
