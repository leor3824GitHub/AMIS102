using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ExportAppXlsx;
using ClosedXML.Excel;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Features;

public sealed class AppXlsxWorkbookTests
{
    [Fact]
    public void Build_MixedSections_WritesFormLayoutTotalsAndPageSetup()
    {
        var bytes = AppXlsxWorkbook.Build(SampleApp(AppPhase.Final), "National Food Authority - Caraga Region", SampleSignatories());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var used = ws.CellsUsed().Select(c => c.GetString()).ToList();

        ws.Cell(1, 1).GetString().ShouldBe("NATIONAL FOOD AUTHORITY - CARAGA REGION");
        ws.Cell(2, 1).GetString().ShouldBe("ANNUAL PROCUREMENT PLAN FOR FY 2027");
        ws.Cell(3, 1).GetString().ShouldContain("☒ FINAL");

        // Section headings appear in form order, each once.
        var general = used.IndexOf(AppSection.GeneralRequirements.ToFormTitle());
        var direct = used.IndexOf(AppSection.DirectAcquisition.ToFormTitle());
        var cse = used.IndexOf(AppSection.CommonUseSupplies.ToFormTitle());
        general.ShouldBeGreaterThan(-1);
        direct.ShouldBeGreaterThan(general);
        cse.ShouldBeGreaterThan(direct);

        // Totals are live formulas: EPA = Security Services only, CSE = PS-DBM row, grand = all four.
        var formulas = ws.CellsUsed(c => c.HasFormula).ToList();
        formulas.Count.ShouldBe(3);
        formulas.Select(c => c.Value.GetNumber()).ShouldBe([21_192_748.44, 580_774d, 22_843_522.44], ignoreOrder: true);

        used.ShouldContain("MARIA SANTOS");
        ws.PageSetup.PageOrientation.ShouldBe(XLPageOrientation.Landscape);
        ws.PageSetup.PaperSize.ShouldBe(XLPaperSize.FolioPaper); // 8.5 × 13 in
    }

    [Fact]
    public void Build_NoItemsNoSignatories_ProducesValidWorkbook()
    {
        var bytes = AppXlsxWorkbook.Build(SampleApp(AppPhase.Indicative, []), agencyName: null, signatories: []);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet(1).Cell(3, 1).GetString().ShouldContain("☒ INDICATIVE");
    }

    private static AnnualProcurementPlanDto SampleApp(AppPhase phase, IReadOnlyList<AppItemDto>? items = null)
    {
        items ??=
        [
            Item(1, "2027 Security Services", "Competitive Bidding", 21_192_748.44m, AppSection.GeneralRequirements, early: true, BidEvaluationCriteria.Lcrb),
            Item(2, "Furniture and Fixtures", "Small Value Procurement", 350_000m, AppSection.GeneralRequirements),
            Item(3, "Utilities", "Direct Acquisition", 720_000m, AppSection.DirectAcquisition),
            Item(4, "Common-Use Supplies", "Procurement from PS-DBM", 580_774m, AppSection.CommonUseSupplies),
        ];

        return new AnnualProcurementPlanDto(
            Guid.NewGuid(), "APP-2027-001", 2027, phase, AppStatus.Approved, 1, true, Guid.NewGuid(), null,
            null, null, null, null, null, null, null, null, null, null,
            items.Sum(i => i.EstimatedBudget), items, DateTimeOffset.UtcNow, null, null);
    }

    private static AppItemDto Item(int no, string title, string mode, decimal budget, AppSection section,
        bool early = false, BidEvaluationCriteria criteria = BidEvaluationCriteria.NotApplicable) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "00B", "Regional Office", no, title, ProjectType.Goods,
            1, "lot", mode, false, "01/2027", "03/2027", "04/2027", "Corporate Operating Budget", budget, null,
            ProjectTitle: title, Section: section, IsEarlyProcurement: early, BidEvaluationCriteria: criteria);

    private static List<ReportSignatoryDto> SampleSignatories() =>
    [
        new(Guid.NewGuid(), "AnnualProcurementPlan", 1, "Prepared by:", "Juan Dela Cruz", "BAC Secretariat", true),
        new(Guid.NewGuid(), "AnnualProcurementPlan", 2, "Recommended by:", "Maria Santos", "BAC Chairperson", true),
        new(Guid.NewGuid(), "AnnualProcurementPlan", 3, "Approved by:", "Pedro Reyes", "Head of the Procuring Entity", true),
    ];
}
