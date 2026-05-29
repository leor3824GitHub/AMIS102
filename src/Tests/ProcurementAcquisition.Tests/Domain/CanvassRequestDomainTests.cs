using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Domain;

public sealed class CanvassRequestDomainTests
{
    [Fact]
    public void Create_WithSubsetOfLines_SnapshotsCoveredItems()
    {
        var canvass = CreateCanvass(itemNos: [1, 3]);

        canvass.Id.ShouldNotBe(Guid.Empty);
        canvass.Status.ShouldBe(CanvassRequestStatus.Open);
        canvass.LineItems.Count.ShouldBe(2);
        canvass.CoveredItemNos.ShouldBe([1, 3]);
    }

    [Fact]
    public void Create_ComputesLineTotalCost()
    {
        var canvass = CreateCanvass(itemNos: [1], quantity: 4, unitCost: 250m);

        canvass.LineItems[0].EstimatedTotalCost.ShouldBe(1000m);
    }

    [Fact]
    public void Create_WithNoLines_Throws()
    {
        Should.Throw<InvalidOperationException>(() => CreateCanvass(itemNos: []));
    }

    [Fact]
    public void Cancel_WhenOpen_ChangesStatusToCancelled()
    {
        var canvass = CreateCanvass(itemNos: [1]);

        canvass.Cancel();

        canvass.Status.ShouldBe(CanvassRequestStatus.Cancelled);
    }

    private static CanvassRequest CreateCanvass(int[] itemNos, decimal quantity = 1m, decimal unitCost = 100m) =>
        CanvassRequest.Create(
            tenantId: "root",
            rivNumber: "RIV-2026-0001",
            purchaseRequestId: Guid.NewGuid(),
            returnDeadline: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            lineItems: itemNos.Select(no => new CanvassRequestLineItemData(
                PrItemNo: no,
                Description: $"Item {no}",
                Unit: "piece",
                Quantity: quantity,
                EstimatedUnitCost: unitCost)));
}
