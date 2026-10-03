using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using Shouldly;
using Xunit;

namespace ProcurementPlanning.Tests.Domain;

public sealed class PpmpPromoteToFinalTests
{
    [Fact]
    public void PromoteToFinal_FromConsolidated_CreatesFinalDraftWithSameItems()
    {
        var ppmp = CreateIndicative();
        ppmp.Submit();
        ppmp.Approve(Guid.NewGuid());
        ppmp.MarkConsolidated();

        var final = ppmp.PromoteToFinal(Guid.NewGuid());

        final.Phase.ShouldBe(PpmpPhase.Final);
        final.Status.ShouldBe(PpmpStatus.Draft);
        final.PreviousVersionId.ShouldBe(ppmp.Id);
        final.VersionChainId.ShouldBe(ppmp.VersionChainId);
        final.Items.Count.ShouldBe(1);
        final.Items[0].EstimatedBudget.ShouldBe(50000m);
    }

    [Fact]
    public void PromoteToFinal_FromDraft_Throws()
    {
        var ppmp = CreateIndicative();

        Should.Throw<InvalidOperationException>(() => ppmp.PromoteToFinal(Guid.NewGuid()));
    }

    private static Ppmp CreateIndicative() =>
        Ppmp.Create(
            ppmpNumber: "PPMP-2027-00B-001",
            fiscalYear: 2027,
            phase: PpmpPhase.Indicative,
            officeCode: "00B",
            endUserUnit: "Admin",
            preparedById: Guid.NewGuid(),
            items:
            [
                new PpmpItemData(
                    "Hybrid Inverter", ProjectType.Goods, 1, "LOT", "Direct Acquisition", false,
                    "01/2027", "03/2027", "04/2027", "General Fund", 50000m, null, null)
            ]);
}
