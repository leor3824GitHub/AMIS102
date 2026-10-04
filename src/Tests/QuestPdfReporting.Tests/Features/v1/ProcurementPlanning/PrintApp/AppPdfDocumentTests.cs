using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Shouldly;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.ProcurementPlanning.PrintApp;

public sealed class AppPdfDocumentTests
{
    static AppPdfDocumentTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Theory]
    [InlineData(AppPhase.Indicative)]
    [InlineData(AppPhase.Final)]
    [InlineData(AppPhase.Updated)]
    public void GeneratePdf_MixedSections_ProducesNonEmptyPdf(AppPhase phase)
    {
        var doc = new AppPdfDocument(SampleApp(phase, SampleItems()), SampleOrg(), SampleSignatories());

        var bytes = doc.GeneratePdf();

        bytes.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_NoItemsNoOrgNoSignatories_RendersWithoutLayoutErrors()
    {
        var doc = new AppPdfDocument(SampleApp(AppPhase.Indicative, []), org: null, signatories: [], paperSize: "a4");

        doc.GeneratePdf().Length.ShouldBeGreaterThan(0);
    }

    private static AnnualProcurementPlanDto SampleApp(AppPhase phase, IReadOnlyList<AppItemDto> items) => new(
        Id: Guid.NewGuid(), AppNumber: "APP-2027-001", FiscalYear: 2027, Phase: phase, Status: AppStatus.Approved,
        VersionNumber: 2, IsCurrentVersion: true, VersionChainId: Guid.NewGuid(), PreviousVersionId: null,
        AmendmentReason: null, AmendedAt: null, AmendedById: null, ConsolidatedById: null, ConsolidatedOn: null,
        ApprovedById: null, ApprovedOn: null, ReturnReason: null, ReturnedAt: null, ReturnedById: null,
        TotalEstimatedBudget: items.Sum(i => i.EstimatedBudget), Items: items,
        CreatedOnUtc: DateTimeOffset.UtcNow, CreatedBy: null, LastModifiedOnUtc: null);

    private static List<AppItemDto> SampleItems() =>
    [
        Item(1, "2027 Security Services", "Competitive Bidding", 21_192_748.44m, AppSection.GeneralRequirements, early: true, BidEvaluationCriteria.Lcrb),
        Item(2, "Furniture and Fixtures", "Small Value Procurement", 350_000m, AppSection.GeneralRequirements),
        Item(3, "Utilities (Water, Electricity, Internet, Telephone, Cable)", "Direct Acquisition", 720_000m, AppSection.DirectAcquisition),
        Item(4, "Common-Use Supplies", "Procurement from PS-DBM", 580_774m, AppSection.CommonUseSupplies),
    ];

    private static AppItemDto Item(int no, string title, string mode, decimal budget, AppSection section,
        bool early = false, BidEvaluationCriteria criteria = BidEvaluationCriteria.NotApplicable) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "00B", "Regional Office", no, title, ProjectType.Goods,
            1, "lot", mode, false, "01/2027", "03/2027", "04/2027", "Corporate Operating Budget", budget, null,
            ProjectTitle: title, Section: section, IsEarlyProcurement: early, BidEvaluationCriteria: criteria);

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(), Name: "National Food Authority - Caraga Region", ShortName: "NFA",
            Address: "Butuan City", LogoUrl: null, AnnexECode: "00B");

    private static List<ReportSignatoryDto> SampleSignatories() =>
    [
        new(Guid.NewGuid(), "AnnualProcurementPlan", 1, "Prepared by:", "Juan Dela Cruz", "Bids and Awards Committee Secretariat", true),
        new(Guid.NewGuid(), "AnnualProcurementPlan", 2, "Recommended by: By the Authority of the Bids and Awards Committee:", "Maria Santos", "Bids and Awards Committee Chairperson", true),
        new(Guid.NewGuid(), "AnnualProcurementPlan", 3, "Approved by:", "Pedro Reyes", "Head of the Procuring Entity", true),
    ];
}
