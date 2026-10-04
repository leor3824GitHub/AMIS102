using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintPpmp;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Shouldly;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.ProcurementPlanning.PrintPpmp;

public sealed class PpmpPdfDocumentTests
{
    static PpmpPdfDocumentTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Theory]
    [InlineData(PpmpPhase.Indicative)]
    [InlineData(PpmpPhase.Final)]
    [InlineData(PpmpPhase.Updated)]
    public void GeneratePdf_WithItemsAndPreparer_ProducesNonEmptyPdf(PpmpPhase phase)
    {
        var doc = new PpmpPdfDocument(SamplePpmp(phase, SampleItems()), SampleOrg(), SamplePreparer());

        doc.GeneratePdf().Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_NoItemsNoOrgNoPreparer_RendersWithoutLayoutErrors()
    {
        var doc = new PpmpPdfDocument(SamplePpmp(PpmpPhase.Indicative, []), org: null, preparer: null, paperSize: "a4");

        doc.GeneratePdf().Length.ShouldBeGreaterThan(0);
    }

    internal static PpmpDto SamplePpmp(PpmpPhase phase, IReadOnlyList<PpmpItemDto> items) => new(
        Guid.NewGuid(), "PPMP-2027-001", 2027, phase, "00B", "Administrative & General Services", PpmpStatus.Approved,
        2, true, Guid.NewGuid(), null, null, null, null, Guid.NewGuid(), null, null, null, null, null, null,
        items.Sum(i => i.EstimatedBudget), items, DateTimeOffset.UtcNow, null, null);

    internal static List<PpmpItemDto> SampleItems() =>
    [
        new(Guid.NewGuid(), 1, "Security guard services for the regional office", ProjectType.Goods, 1, "LOT",
            "Competitive Bidding", true, "11/2026", "12/2026", "01/2027", "Corporate Operating Budget", 21_192_748.44m,
            "Terms of Reference", null),
        new(Guid.NewGuid(), 2, "Consulting services — warehouse design", ProjectType.ConsultingServices, 1, "LOT",
            "Competitive Bidding", false, "03/2027", "05/2027", "06/2027", "GAA", 2_000_000m, null, "Subject to approval"),
    ];

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(), Name: "National Food Authority - Caraga Region", ShortName: "NFA",
            Address: "Butuan City", LogoUrl: null, AnnexECode: "00B");

    private static EmployeeReferenceDto SamplePreparer() =>
        new(Guid.NewGuid(), "EMP-001", null, "Juan", "Dela Cruz", null, Guid.NewGuid(), "OFF", "Regional Office", null,
            Guid.NewGuid(), "AGS", "Administrative & General Services", Guid.NewGuid(), "POS", "Administrative Officer", true);
}
