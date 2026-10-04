using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Domain;

/// <summary>GPPB APP form (RA 12009) columns: defaults and carry-over from PPMP item → APP line item.</summary>
public sealed class AppGppbFieldsTests
{
    [Theory]
    [InlineData("Competitive Bidding", AppSection.GeneralRequirements)]
    [InlineData("Small Value Procurement", AppSection.GeneralRequirements)]
    [InlineData("Direct Acquisition", AppSection.DirectAcquisition)]
    [InlineData("Procurement from PS-DBM", AppSection.CommonUseSupplies)]
    [InlineData("", AppSection.GeneralRequirements)]
    public void SuggestSection_ByMode_ReturnsExpectedSection(string mode, AppSection expected) =>
        ProcurementPlanRules.SuggestSection(mode).ShouldBe(expected);

    [Theory]
    [InlineData("Competitive Bidding", ProjectType.Goods, BidEvaluationCriteria.Lcrb)]
    [InlineData("Public Bidding", ProjectType.Infrastructure, BidEvaluationCriteria.Lcrb)]
    [InlineData("Competitive Bidding", ProjectType.ConsultingServices, BidEvaluationCriteria.Hrrb)]
    [InlineData("Small Value Procurement", ProjectType.Goods, BidEvaluationCriteria.NotApplicable)]
    [InlineData("Direct Acquisition", ProjectType.Goods, BidEvaluationCriteria.NotApplicable)]
    public void SuggestCriteria_ByModeAndType_ReturnsExpectedCriteria(string mode, ProjectType type, BidEvaluationCriteria expected) =>
        ProcurementPlanRules.SuggestCriteria(mode, type).ShouldBe(expected);

    [Fact]
    public void PpmpItem_WithoutProjectTitle_FallsBackToDescription()
    {
        var ppmp = CreatePpmp(Item("Pest Control Equipment", projectTitle: null));

        ppmp.Items.Single().ProjectTitle.ShouldBe("Pest Control Equipment");
    }

    [Fact]
    public void ConsolidatePpmps_CarriesGppbFieldsToLineItem()
    {
        var ppmp = CreatePpmp(Item("Security guard services for 2027", projectTitle: "2027 Security Services",
            section: AppSection.GeneralRequirements, early: true, criteria: BidEvaluationCriteria.Lcrb, strategy: "Framework agreement"));
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        var app = AnnualProcurementPlan.Create("APP-2027-001", 2027, AppPhase.Indicative);

        app.ConsolidatePpmps([ppmp], Guid.NewGuid());

        var line = app.LineItems.Single();
        line.ProjectTitle.ShouldBe("2027 Security Services");
        line.Section.ShouldBe(AppSection.GeneralRequirements);
        line.IsEarlyProcurement.ShouldBeTrue();
        line.BidEvaluationCriteria.ShouldBe(BidEvaluationCriteria.Lcrb);
        line.ProcurementStrategy.ShouldBe("Framework agreement");
    }

    private static PpmpItemData Item(
        string description,
        string? projectTitle,
        AppSection section = AppSection.GeneralRequirements,
        bool early = false,
        BidEvaluationCriteria criteria = BidEvaluationCriteria.NotApplicable,
        string? strategy = null) =>
        new(GeneralDescription: description,
            ProjectType: ProjectType.Goods,
            Quantity: 1,
            Unit: "lot",
            ModeOfProcurement: "Competitive Bidding",
            PreProcurementConference: false,
            ProcurementStart: "01/2027",
            ProcurementEnd: "03/2027",
            ExpectedDelivery: "04/2027",
            SourceOfFunds: "Corporate Operating Budget",
            EstimatedBudget: 1_000m,
            SupportingDocuments: null,
            Remarks: null,
            ProjectTitle: projectTitle,
            Section: section,
            IsEarlyProcurement: early,
            BidEvaluationCriteria: criteria,
            ProcurementStrategy: strategy);

    private static Ppmp CreatePpmp(PpmpItemData item) =>
        Ppmp.Create(
            ppmpNumber: "PPMP-2027-001",
            fiscalYear: 2027,
            phase: PpmpPhase.Indicative,
            officeCode: "00B",
            endUserUnit: "Administrative & General Services",
            preparedById: Guid.NewGuid(),
            items: [item]);
}
