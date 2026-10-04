using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ExportPpmpXlsx;
using ClosedXML.Excel;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Features;

public sealed class PpmpXlsxWorkbookTests
{
    [Fact]
    public void Build_WithItems_WritesFormLayoutTotalAndPreparer()
    {
        var bytes = PpmpXlsxWorkbook.Build(SamplePpmp(PpmpPhase.Updated), "National Food Authority - Caraga Region", SamplePreparer());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var used = ws.CellsUsed().Select(c => c.GetString()).ToList();

        ws.Cell(2, 1).GetString().ShouldBe("PROJECT PROCUREMENT MANAGEMENT PLAN (PPMP) NO. PPMP-2027-001");
        ws.Cell(3, 1).GetString().ShouldContain("☒ FINAL (Updated — Version No. 2)");
        used.ShouldContain("Administrative & General Services");
        used.ShouldContain("1 LOT");
        used.ShouldContain("Terms of Reference");
        used.ShouldContain("JUAN DELA CRUZ");

        var total = ws.CellsUsed(c => c.HasFormula).Single();
        total.Value.GetNumber().ShouldBe(23_192_748.44);

        ws.PageSetup.PageOrientation.ShouldBe(XLPageOrientation.Landscape);
        ws.PageSetup.PaperSize.ShouldBe(XLPaperSize.FolioPaper);
    }

    [Fact]
    public void Build_NoItemsNoPreparer_ProducesValidWorkbook()
    {
        var bytes = PpmpXlsxWorkbook.Build(SamplePpmp(PpmpPhase.Indicative, []), agencyName: null, preparer: null);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet(1).Cell(3, 1).GetString().ShouldContain("☒ INDICATIVE");
    }

    private static PpmpDto SamplePpmp(PpmpPhase phase, IReadOnlyList<PpmpItemDto>? items = null)
    {
        items ??=
        [
            new(Guid.NewGuid(), 1, "Security guard services", ProjectType.Goods, 1, "LOT", "Competitive Bidding", true,
                "11/2026", "12/2026", "01/2027", "Corporate Operating Budget", 21_192_748.44m, "Terms of Reference", null),
            new(Guid.NewGuid(), 2, "Warehouse design", ProjectType.ConsultingServices, 1, "LOT", "Competitive Bidding", false,
                "03/2027", "05/2027", "06/2027", "GAA", 2_000_000m, null, null),
        ];

        return new PpmpDto(
            Guid.NewGuid(), "PPMP-2027-001", 2027, phase, "00B", "Administrative & General Services", PpmpStatus.Approved,
            2, true, Guid.NewGuid(), null, null, null, null, Guid.NewGuid(), null, null, null, null, null, null,
            items.Sum(i => i.EstimatedBudget), items, DateTimeOffset.UtcNow, null, null);
    }

    private static EmployeeReferenceDto SamplePreparer() =>
        new(Guid.NewGuid(), "EMP-001", null, "Juan", "Dela Cruz", null, Guid.NewGuid(), "OFF", "Regional Office", null,
            Guid.NewGuid(), "AGS", "Administrative & General Services", Guid.NewGuid(), "POS", "Administrative Officer", true);
}
