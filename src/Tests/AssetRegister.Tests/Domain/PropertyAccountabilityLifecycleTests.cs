using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

public sealed class PropertyAccountabilityLifecycleTests
{
    [Fact]
    public void Issue_StartsInPendingAcceptance()
    {
        var acc = NewParWith(NewAsset("001"));

        acc.Status.ShouldBe(AccountabilityStatus.PendingAcceptance);
        acc.AcceptedOn.ShouldBeNull();
    }

    [Fact]
    public void Accept_MovesPendingToActiveAndStampsDate()
    {
        var acc = NewParWith(NewAsset("001"));
        var acceptedOn = new DateOnly(2026, 6, 20);

        acc.Accept(acceptedOn);

        acc.Status.ShouldBe(AccountabilityStatus.Active);
        acc.AcceptedOn.ShouldBe(acceptedOn);
    }

    [Fact]
    public void Accept_WhenAlreadyActive_Throws()
    {
        var acc = NewParWith(NewAsset("001"));
        acc.Accept(new DateOnly(2026, 6, 20));

        Should.Throw<InvalidOperationException>(() => acc.Accept(new DateOnly(2026, 6, 21)));
    }

    [Fact]
    public void UpdateHeader_AfterAccept_Throws()
    {
        var acc = NewParWith(NewAsset("001"));
        acc.Accept(new DateOnly(2026, 6, 20));

        Should.Throw<InvalidOperationException>(() =>
            acc.UpdateHeader("02", EmployeeRef.Create(Guid.NewGuid(), "New Recv", null), new DateOnly(2026, 6, 16), null));
    }

    [Fact]
    public void UpdateHeader_WhilePending_AppliesChanges()
    {
        var acc = NewParWith(NewAsset("001"));
        var newRecv = EmployeeRef.Create(Guid.NewGuid(), "New Recipient", "Clerk");

        acc.UpdateHeader("02", newRecv, new DateOnly(2026, 7, 1), null);

        acc.FundCluster.ShouldBe("02");
        acc.ReceivedBy.PrintedName.ShouldBe("New Recipient");
        acc.IssuedOn.ShouldBe(new DateOnly(2026, 7, 1));
        // PAR expiry is server-derived: issued + 3 years.
        acc.ExpiresOn.ShouldBe(new DateOnly(2029, 7, 1));
    }

    [Fact]
    public void RemoveLine_WhenOnlyOneLine_Throws()
    {
        var acc = NewParWith(NewAsset("001"));
        var lineId = acc.Lines.First().Id;

        Should.Throw<InvalidOperationException>(() => acc.RemoveLine(lineId));
    }

    [Fact]
    public void AddThenRemoveLine_WhilePending_Succeeds()
    {
        var acc = NewParWith(NewAsset("001"));
        var second = NewAsset("002");

        acc.AddLine(second, "2", null, null);
        acc.Lines.Count.ShouldBe(2);

        var freedAssetId = acc.RemoveLine(acc.Lines.First().Id);
        acc.Lines.Count.ShouldBe(1);
        freedAssetId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void AddLine_WithWrongAssetType_Throws()
    {
        var acc = NewParWith(NewAsset("001"));   // PAR expects PPE
        var seAsset = NewSeAsset("003");

        Should.Throw<InvalidOperationException>(() => acc.AddLine(seAsset, "2", null, null));
    }

    [Fact]
    public void EnsureDeletableDraft_Throws_OnceAccepted()
    {
        var acc = NewParWith(NewAsset("001"));
        Should.NotThrow(acc.EnsureDeletableDraft);

        acc.Accept(new DateOnly(2026, 6, 20));
        Should.Throw<InvalidOperationException>(acc.EnsureDeletableDraft);
    }

    private static PropertyAccountability NewParWith(AssetRegistry asset) =>
        PropertyAccountability.Issue(
            tenantId: "root",
            type: AccountabilityType.PPE_PAR,
            documentNo: "PAR-2026-06-0001",
            fundCluster: "01",
            issuedBy: EmployeeRef.Create(Guid.NewGuid(), "Supply Officer", "Officer"),
            receivedBy: EmployeeRef.Create(Guid.NewGuid(), "End User", "Engineer"),
            issuedOn: new DateOnly(2026, 6, 16),
            expiresOn: null,
            lines: [(asset, "1", null, null)]);

    private static AssetRegistry NewAsset(string suffix) => NewAsset(suffix, AssetType.PPE, AssetCategory.PPE);

    private static AssetRegistry NewAsset(string suffix, AssetType assetType, AssetCategory category)
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root",
            code: $"DESK-{suffix}",
            description: "Office Desk",
            defaultPropertyClass: "07-PPE",
            defaultCategoryCode: "DSK",
            defaultUnit: "pc",
            uacsObjectCode: "10405030",
            estimatedUsefulLifeYears: 10);

        var propertyNo = PropertyNumber.Create($"2026-NFA-00B-07-DSK-{suffix}");

        return AssetRegistry.Register(
            tenantId: "root",
            catalog: catalog,
            assetType: assetType,
            category: category,
            propertyNo: propertyNo,
            description: "Office Desk",
            serialNo: null,
            brand: null,
            model: null,
            fundCluster: "01",
            acquisitionDate: new DateOnly(2026, 1, 15),
            unitCost: 5000m,
            sourceIARId: null,
            sourcePurchaseOrderId: null);
    }

    private static AssetRegistry NewSeAsset(string suffix) => NewAsset(suffix, AssetType.SE, AssetCategory.LowValuedSemi);
}
