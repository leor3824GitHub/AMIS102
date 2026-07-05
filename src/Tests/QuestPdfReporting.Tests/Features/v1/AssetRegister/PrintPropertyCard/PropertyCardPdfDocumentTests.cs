using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Shouldly;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.AssetRegister.PrintPropertyCard;

public sealed class PropertyCardPdfDocumentTests
{
    // 1×1 transparent PNG — smallest valid raster QuestPDF/SkiaSharp can decode.
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    static PropertyCardPdfDocumentTests() =>
        QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void GeneratePdf_WithEmbeddedPhoto_ProducesNonEmptyPdf()
    {
        var doc = new AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard
            .PropertyCardPdfDocument(SampleCard(PngDataUrl), SampleOrg());

        var bytes = doc.GeneratePdf();

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_WithoutPhoto_ProducesNonEmptyPdf()
    {
        var doc = new AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard
            .PropertyCardPdfDocument(SampleCard(imageUrl: null), org: null);

        var bytes = doc.GeneratePdf();

        bytes.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_WithUnparseablePhoto_FallsBackAndStillRenders()
    {
        // A malformed data URL must not crash the whole card — DecodePhoto returns null and the
        // header simply prints without a photo.
        var doc = new AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard
            .PropertyCardPdfDocument(SampleCard("data:image/png;base64,not-valid-base64!!"), SampleOrg());

        var bytes = doc.GeneratePdf();

        bytes.Length.ShouldBeGreaterThan(0);
    }

    private static PropertyCardDto SampleCard(string? imageUrl) => new(
        AssetRegistryId: Guid.NewGuid(),
        PropertyNo: "2026-06-LT-0001",
        Description: "Toyota Innova",
        AssetType: AssetType.PPE,
        Unit: "unit",
        AcquisitionDate: new DateOnly(2026, 7, 4),
        AcquisitionCost: 950_000m,
        CurrentState: LifecycleState.Assigned,
        Movements:
        [
            new(Date: new DateOnly(2026, 7, 4), MovementType: AssetMovementType.Acquired,
                Source: MovementSource.Receiving, DocumentNo: "PPERR-2026-001", DocumentId: Guid.NewGuid(),
                Party: "ABC Motors", Amount: 950_000m, Remarks: "PPERR · Purchase"),
            new(Date: new DateOnly(2026, 7, 10), MovementType: AssetMovementType.Issued,
                Source: MovementSource.Accountability, DocumentNo: "PAR-2026-014", DocumentId: Guid.NewGuid(),
                Party: "Juan dela Cruz", Amount: 950_000m, Remarks: "PPE_PAR")
        ],
        ImageUrl: imageUrl);

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");
}
