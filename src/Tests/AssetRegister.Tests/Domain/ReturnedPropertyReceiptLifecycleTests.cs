using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// Covers the <see cref="ReturnedPropertyReceipt"/> state machine
/// (Pending → Inspected → Accepted / Rejected / Cancelled) and its guard rails.
/// Asset / accountability mutation is owned by the handler, so it is not exercised here.
/// </summary>
public sealed class ReturnedPropertyReceiptLifecycleTests
{
    [Fact]
    public void Create_StartsPending_WithNoNumberAndNoReceiver()
    {
        var receipt = NewReceiptWith(out _, "001");

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Pending);
        receipt.ReceiptNo.ShouldBeNull();
        receipt.ReceivedBy.ShouldBeNull();
        receipt.InspectedBy.ShouldBeNull();
        // The inspector nominated by the requester is captured up front, distinct from who actually inspects.
        receipt.AssignedInspector.ShouldNotBeNull();
        receipt.AssignedInspector.PrintedName.ShouldBe("Property Inspector");
        receipt.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void Inspect_FromPending_RecordsConditionsAndMovesToInspected()
    {
        var receipt = NewReceiptWith(out var items, "001", "002");
        var conditions = new Dictionary<Guid, AssetCondition>
        {
            [items[0].Id] = AssetCondition.InGoodCondition,
            [items[1].Id] = AssetCondition.NeedingRepair
        };

        receipt.Inspect(Inspector(), conditions, "All accounted for.");

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Inspected);
        receipt.InspectedBy.ShouldNotBeNull();
        receipt.InspectionRemarks.ShouldBe("All accounted for.");
        receipt.InspectedOnUtc.ShouldNotBeNull();
        receipt.Items.Single(i => i.Id == items[0].Id).InspectedCondition.ShouldBe(AssetCondition.InGoodCondition);
        receipt.Items.Single(i => i.Id == items[1].Id).InspectedCondition.ShouldBe(AssetCondition.NeedingRepair);
    }

    [Fact]
    public void Inspect_WhenAnItemIsMissingItsCondition_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001", "002");
        var partial = new Dictionary<Guid, AssetCondition>
        {
            [items[0].Id] = AssetCondition.InGoodCondition
            // items[1] deliberately omitted
        };

        Should.Throw<InvalidOperationException>(() => receipt.Inspect(Inspector(), partial, null));
        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Pending);
    }

    [Fact]
    public void Inspect_WhenNotPending_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        var conditions = new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition };
        receipt.Inspect(Inspector(), conditions, null);

        Should.Throw<InvalidOperationException>(() => receipt.Inspect(Inspector(), conditions, null));
    }

    [Fact]
    public void Inspect_BySomeoneOtherThanAssignedInspector_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        var conditions = new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition };
        var someoneElse = EmployeeRef.Create(Guid.NewGuid(), "Other Person", "Clerk");

        // Authorization failure (not the assigned inspector) — the handler maps this to a 403.
        Should.Throw<UnauthorizedAccessException>(() => receipt.Inspect(someoneElse, conditions, null));
        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Pending);
    }

    [Fact]
    public void ReassignInspector_WhilePending_ReplacesNominatedInspectorAndLetsNewInspectorProceed()
    {
        var receipt = NewReceiptWith(out var items, "001");
        var newInspector = EmployeeRef.Create(Guid.NewGuid(), "New Inspector", "Senior Inspector");

        receipt.ReassignInspector(newInspector);

        receipt.AssignedInspector.EmployeeId.ShouldBe(newInspector.EmployeeId);
        receipt.AssignedInspector.PrintedName.ShouldBe("New Inspector");

        // The originally-nominated inspector can no longer inspect; the new one can.
        var conditions = new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition };
        Should.Throw<UnauthorizedAccessException>(() => receipt.Inspect(Inspector(), conditions, null));
        Should.NotThrow(() => receipt.Inspect(newInspector, conditions, null));
        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Inspected);
    }

    [Fact]
    public void ReassignInspector_AfterInspected_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition }, null);

        Should.Throw<InvalidOperationException>(() =>
            receipt.ReassignInspector(EmployeeRef.Create(Guid.NewGuid(), "Late Inspector", null)));
    }

    [Fact]
    public void Accept_FromInspected_AssignsNumberAndReceiverAndStamps()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition }, null);

        var receiver = EmployeeRef.Create(Guid.NewGuid(), "Supply Officer", "Custodian");
        receipt.Accept("RRP-2026-00001", receiver);

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Accepted);
        receipt.ReceiptNo.ShouldBe("RRP-2026-00001");
        receipt.ReceivedBy!.PrintedName.ShouldBe("Supply Officer");
        receipt.AcceptedOnUtc.ShouldNotBeNull();
        receipt.ResolvedOnUtc.ShouldBe(receipt.AcceptedOnUtc);
    }

    [Fact]
    public void Accept_WhenStillPending_Throws()
    {
        var receipt = NewReceiptWith(out _, "001");

        Should.Throw<InvalidOperationException>(() =>
            receipt.Accept("RRP-2026-00001", EmployeeRef.Create(Guid.NewGuid(), "Custodian", null)));
    }

    [Fact]
    public void Accept_WithBlankReceiptNo_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition }, null);

        Should.Throw<InvalidOperationException>(() =>
            receipt.Accept("  ", EmployeeRef.Create(Guid.NewGuid(), "Custodian", null)));
    }

    [Fact]
    public void Reject_FromPending_Succeeds()
    {
        var receipt = NewReceiptWith(out _, "001");

        receipt.Reject("Item not physically present.");

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Rejected);
        receipt.RejectionReason.ShouldBe("Item not physically present.");
        receipt.ResolvedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Reject_FromInspected_Succeeds()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.Unserviceable }, null);

        receipt.Reject("Condition does not match request.");

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Rejected);
    }

    [Fact]
    public void Reject_WhenAccepted_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition }, null);
        receipt.Accept("RRP-2026-00001", EmployeeRef.Create(Guid.NewGuid(), "Custodian", null));

        Should.Throw<InvalidOperationException>(() => receipt.Reject("too late"));
    }

    [Fact]
    public void Reject_WithBlankReason_Throws()
    {
        var receipt = NewReceiptWith(out _, "001");

        Should.Throw<InvalidOperationException>(() => receipt.Reject("   "));
    }

    [Fact]
    public void Cancel_FromPending_Succeeds_AndAllowsNullReason()
    {
        var receipt = NewReceiptWith(out _, "001");

        receipt.Cancel(null);

        receipt.Status.ShouldBe(ReturnedPropertyReceiptStatus.Cancelled);
        receipt.CancellationReason.ShouldBeNull();
        receipt.ResolvedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Cancel_WhenAccepted_Throws()
    {
        var receipt = NewReceiptWith(out var items, "001");
        receipt.Inspect(Inspector(), new Dictionary<Guid, AssetCondition> { [items[0].Id] = AssetCondition.InGoodCondition }, null);
        receipt.Accept("RRP-2026-00001", EmployeeRef.Create(Guid.NewGuid(), "Custodian", null));

        Should.Throw<InvalidOperationException>(() => receipt.Cancel("changed my mind"));
    }

    // The inspector nominated on the receipt and the one who performs the inspection must be the
    // same person — share one id so the assigned-inspector guard is satisfied in the happy-path tests.
    private static readonly Guid AssignedInspectorId = Guid.NewGuid();

    private static EmployeeRef Inspector() => EmployeeRef.Create(AssignedInspectorId, "Property Inspector", "Inspector");

    private static ReturnedPropertyReceipt NewReceiptWith(
        out List<ReturnedPropertyReceiptItem> items, params string[] propertyNoSuffixes)
    {
        var receipt = ReturnedPropertyReceipt.Create(
            tenantId: "root",
            receiptType: ReturnedPropertyReceiptType.RRP,
            date: new DateOnly(2026, 6, 17),
            accountabilityId: Guid.NewGuid(),
            accountabilityDocumentNo: "PAR-2026-06-0001",
            returnedBy: EmployeeRef.Create(Guid.NewGuid(), "End User", "Engineer"),
            assignedInspector: EmployeeRef.Create(AssignedInspectorId, "Property Inspector", "Inspector"),
            remarks: null);

        var itemNo = 1;
        foreach (var suffix in propertyNoSuffixes)
            receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), itemNo++, NewSnapshot(suffix));

        items = receipt.Items.ToList();
        return receipt;
    }

    private static AssetSnapshot NewSnapshot(string suffix) => AssetSnapshot.Create(
        propertyNo: $"2026-NFA-00B-07-DSK-{suffix}",
        description: "Office Desk",
        assetType: AssetType.PPE,
        unitCost: 5000m,
        unit: "pc",
        estimatedUsefulLifeYears: 10,
        acquisitionDate: new DateOnly(2026, 1, 15),
        uacsObjectCode: "10405030",
        serialNo: null,
        brand: null,
        model: null,
        netBookValue: 4500m);
}
