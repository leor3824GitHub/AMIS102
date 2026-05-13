using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Domain;

public sealed class AnnualProcurementPlanDomainTests
{
    [Fact]
    public void Create_ValidInput_CreatesDraftPlan()
    {
        var app = AnnualProcurementPlan.Create("APP-2025-001", 2025, AppPhase.Indicative);

        app.Id.ShouldNotBe(Guid.Empty);
        app.Status.ShouldBe(AppStatus.Draft);
        app.VersionNumber.ShouldBe(1);
        app.IsCurrentVersion.ShouldBeTrue();
        app.VersionChainId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void ValidateForConsolidation_WhenDraft_DoesNotThrow()
    {
        var app = AnnualProcurementPlan.Create("APP-2025-001", 2025, AppPhase.Indicative);
        var act = () => app.ValidateForConsolidation([]);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ConsolidatePpmps_WhenPublished_Throws()
    {
        var app = AnnualProcurementPlan.Create("APP-2025-001", 2025, AppPhase.Indicative);
        var ppmp = CreateApprovedPpmp(app.FiscalYear, PpmpPhase.Indicative);

        app.ConsolidatePpmps([ppmp], Guid.NewGuid());
        app.Publish();

        var act = () => app.ConsolidatePpmps([ppmp], Guid.NewGuid());

        act.ShouldThrow<InvalidOperationException>();
    }

    private static Ppmp CreateApprovedPpmp(int fiscalYear, PpmpPhase phase)
    {
        var ppmp = Ppmp.Create(
            ppmpNumber: $"PPMP-{fiscalYear}-001",
            fiscalYear: fiscalYear,
            phase: phase,
            officeCode: "OFFICE-1",
            endUserUnit: "End User Unit",
            preparedById: Guid.NewGuid(),
            items:
            [
                new PpmpItemData(
                    GeneralDescription: "Office supplies",
                    ProjectType: ProjectType.Goods,
                    Quantity: 1,
                    Unit: "lot",
                    ModeOfProcurement: "Shopping",
                    PreProcurementConference: false,
                    ProcurementStart: "Jan",
                    ProcurementEnd: "Feb",
                    ExpectedDelivery: "Mar",
                    SourceOfFunds: "General Fund",
                    EstimatedBudget: 1000m,
                    SupportingDocuments: null,
                    Remarks: null)
            ]);

        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());

        return ppmp;
    }
}
