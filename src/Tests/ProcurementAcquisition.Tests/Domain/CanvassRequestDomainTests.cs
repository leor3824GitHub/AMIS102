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

    [Fact]
    public void AwardLines_FreezesCommitteeSignatories()
    {
        var canvass = CreateCanvass(itemNos: [1]);
        var committee = new[]
        {
            CanvassAwardSignatory.Create(6, "Maria Santos", "Assistant Regional Manager II"),
            CanvassAwardSignatory.Create(2, "Jane Cruz", "Accountant IV"),
        };

        canvass.AwardLines(Awards((1, Guid.NewGuid(), Guid.NewGuid(), 100m)), committee);

        canvass.Status.ShouldBe(CanvassRequestStatus.Awarded);
        canvass.AwardSignatories.Count.ShouldBe(2);
        canvass.AwardSignatories.ShouldContain(s => s.SortOrder == 6 && s.Name == "Maria Santos");
    }

    [Fact]
    public void AwardLines_DifferentSuppliersPerLine_SetsPerLineWinnersAndNullAggregate()
    {
        var canvass = CreateCanvass(itemNos: [1, 2]);
        var supplierA = Guid.NewGuid();
        var supplierB = Guid.NewGuid();

        canvass.AwardLines(Awards(
            (1, Guid.NewGuid(), supplierA, 120m),
            (2, Guid.NewGuid(), supplierB, 80m)));

        canvass.Status.ShouldBe(CanvassRequestStatus.Awarded);
        canvass.LineItems.Single(l => l.PrItemNo == 1).AwardedSupplierId.ShouldBe(supplierA);
        canvass.LineItems.Single(l => l.PrItemNo == 1).AwardedUnitPrice.ShouldBe(120m);
        canvass.LineItems.Single(l => l.PrItemNo == 2).AwardedSupplierId.ShouldBe(supplierB);
        // Multiple winners → no single aggregate supplier.
        canvass.AwardedSupplierId.ShouldBeNull();
    }

    [Fact]
    public void AwardLines_SingleSupplierSweep_SetsAggregateSupplier()
    {
        var canvass = CreateCanvass(itemNos: [1, 2]);
        var supplier = Guid.NewGuid();

        canvass.AwardLines(Awards(
            (1, Guid.NewGuid(), supplier, 120m),
            (2, Guid.NewGuid(), supplier, 80m)));

        canvass.AwardedSupplierId.ShouldBe(supplier);
    }

    [Fact]
    public void AwardLines_PartialAward_Throws()
    {
        var canvass = CreateCanvass(itemNos: [1, 2]);

        Should.Throw<InvalidOperationException>(() =>
            canvass.AwardLines(Awards((1, Guid.NewGuid(), Guid.NewGuid(), 100m))));
    }

    [Fact]
    public void AwardLines_WhenAlreadyAwarded_Throws()
    {
        var canvass = CreateCanvass(itemNos: [1]);
        canvass.AwardLines(Awards((1, Guid.NewGuid(), Guid.NewGuid(), 100m)));

        Should.Throw<InvalidOperationException>(() =>
            canvass.AwardLines(Awards((1, Guid.NewGuid(), Guid.NewGuid(), 90m))));
    }

    private static Dictionary<int, (Guid QuotationId, Guid SupplierId, decimal UnitPrice)> Awards(
        params (int PrItemNo, Guid QuotationId, Guid SupplierId, decimal UnitPrice)[] awards) =>
        awards.ToDictionary(a => a.PrItemNo, a => (a.QuotationId, a.SupplierId, a.UnitPrice));

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
