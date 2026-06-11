using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Counting;
using AMIS.Modules.AssetRegister.Domain.Events;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

/// <summary>
/// Covers the "balance = found" close behaviour at the domain level (COA Circular 2020-006):
/// not-found assets drop out of the active balance, and the close invariant still guards
/// unmaterialized found-at-station entries.
/// </summary>
public sealed class PhysicalCountCloseTests
{
    [Theory]
    [InlineData(true)]   // from Available
    [InlineData(false)]  // from Assigned
    public void MarkMissingFromCount_FromActiveBalance_MovesToUnderInvestigationAndRaisesEvent(bool fromAvailable)
    {
        var asset = NewAsset();
        if (!fromAvailable)
            asset.AssignTo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        asset.ClearDomainEvents();

        asset.MarkMissingFromCount(Guid.NewGuid());

        asset.LifecycleState.ShouldBe(LifecycleState.UnderInvestigation);
        asset.DomainEvents.OfType<AssetReportedMissingFromCountEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkMissingFromCount_WhenUnderInvestigation_IsNoOp()
    {
        var asset = NewAsset();
        asset.MarkUnderInvestigation(Guid.NewGuid());
        asset.ClearDomainEvents();

        asset.MarkMissingFromCount(Guid.NewGuid());

        asset.LifecycleState.ShouldBe(LifecycleState.UnderInvestigation);
        asset.DomainEvents.OfType<AssetReportedMissingFromCountEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MarkMissingFromCount_WhenTransferredOut_IsNoOp()
    {
        var asset = NewAsset();
        asset.MarkTransferredOut(Guid.NewGuid(), "PPEIR-2026-05-0001", IssuanceReportType.PPEIR);
        asset.ClearDomainEvents();

        asset.MarkMissingFromCount(Guid.NewGuid());

        asset.LifecycleState.ShouldBe(LifecycleState.TransferredOut);
        asset.DomainEvents.OfType<AssetReportedMissingFromCountEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MarkMissingFromCount_WhenDisposed_IsNoOp()
    {
        var asset = NewAsset();
        asset.MarkUnserviceable(Guid.NewGuid());
        asset.Dispose(Guid.NewGuid(), DisposalMethod.Sale);
        asset.ClearDomainEvents();

        asset.MarkMissingFromCount(Guid.NewGuid());

        asset.LifecycleState.ShouldBe(LifecycleState.Disposed);
        asset.DomainEvents.OfType<AssetReportedMissingFromCountEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MarkUnderInvestigation_FromUnderInvestigation_IsAllowedAndRaisesLostEvent()
    {
        var asset = NewAsset();
        asset.MarkMissingFromCount(Guid.NewGuid()); // count flagged it first → UnderInvestigation
        asset.ClearDomainEvents();

        // A formal RLSDDSP can still attach to the already-flagged asset.
        Should.NotThrow(() => asset.MarkUnderInvestigation(Guid.NewGuid()));
        asset.LifecycleState.ShouldBe(LifecycleState.UnderInvestigation);
        asset.DomainEvents.OfType<AssetLostEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Close_WithUnmaterializedFoundAtStationEntry_Throws()
    {
        var session = NewReconciledSessionWithFoundAtStation();

        Should.Throw<InvalidOperationException>(() =>
            session.Close(Approver(), witnessedBy: null, new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void AddFoundAtStationEntry_RoundTripsProposedIdentityFields()
    {
        var session = NewOngoingSession();
        var catalogId = Guid.NewGuid();

        session.AddFoundAtStationEntry(
            article: "Found Printer", unit: "pc", unitCost: 12000m, locationId: Guid.NewGuid(),
            proposedPropertyClass: "07-PPE", proposedCategoryCode: "PRN",
            proposedAcquisitionDate: new DateOnly(2026, 1, 10), proposedUnitCost: 12000m,
            proposedPropertyNo: "2026-NFA-00B-07-PRN-009", proposedCatalogItemId: catalogId,
            scannedByEmployeeId: null, remarks: null);

        var entry = session.Entries.ShouldHaveSingleItem();
        entry.ProposedPropertyNo.ShouldBe("2026-NFA-00B-07-PRN-009");
        entry.ProposedCatalogItemId.ShouldBe(catalogId);
        entry.Condition.ShouldBe(PhysicalCountCondition.FoundAtStation);
        entry.AssetRegistryId.ShouldBeNull();
    }

    private static PhysicalCountSession NewOngoingSession()
    {
        var session = PhysicalCountSession.Start(
            tenantId: "root", code: "PCS-2026-0001", scope: PhysicalCountScope.PPEOnly,
            fundCluster: "01", asAt: new DateOnly(2026, 1, 31), startedOn: new DateOnly(2026, 1, 31),
            conductedBy: [EmployeeRef.Create(Guid.NewGuid(), "Juan Dela Cruz", "Supply Officer")],
            remarks: null, officeOrderNo: null);
        session.Freeze("OO-2026-001", DateTimeOffset.UtcNow);
        return session;
    }

    private static PhysicalCountSession NewReconciledSessionWithFoundAtStation()
    {
        var session = NewOngoingSession();
        session.AddFoundAtStationEntry(
            article: "Found Printer", unit: "pc", unitCost: 12000m, locationId: Guid.NewGuid(),
            proposedPropertyClass: null, proposedCategoryCode: null,
            proposedAcquisitionDate: null, proposedUnitCost: null,
            proposedPropertyNo: null, proposedCatalogItemId: null,
            scannedByEmployeeId: null, remarks: null);
        session.Reconcile();
        return session;
    }

    private static EmployeeRef Approver() =>
        EmployeeRef.Create(Guid.NewGuid(), "Maria Santos", "Division Chief");

    private static AssetRegistry NewAsset()
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root", code: "DESK-001", description: "Office Desk",
            defaultPropertyClass: "07-PPE", defaultCategoryCode: "DSK", defaultUnit: "pc",
            uacsObjectCode: "10405030", estimatedUsefulLifeYears: 10);

        return AssetRegistry.Register(
            tenantId: "root", catalog: catalog, assetType: AssetType.PPE, category: AssetCategory.PPE,
            propertyNo: PropertyNumber.Create("2026-NFA-00B-07-DSK-001"), description: "Office Desk",
            serialNo: null, brand: null, model: null, fundCluster: "01",
            acquisitionDate: new DateOnly(2026, 1, 15), unitCost: 5000m,
            sourceIARId: null, sourcePurchaseOrderId: null);
    }
}
