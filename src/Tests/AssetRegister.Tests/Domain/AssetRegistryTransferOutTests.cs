using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

public sealed class AssetRegistryTransferOutTests
{
    [Fact]
    public void MarkTransferredOut_FromAvailable_TransitionsState()
    {
        var asset = NewAsset();

        asset.MarkTransferredOut(Guid.NewGuid(), "PPEIR-2026-05-0001", IssuanceReportType.PPEIR);

        asset.LifecycleState.ShouldBe(LifecycleState.TransferredOut);
    }

    [Fact]
    public void MarkTransferredOut_FromAssigned_ClearsCurrentAccountability()
    {
        var asset = NewAsset();
        asset.AssignTo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        asset.MarkTransferredOut(Guid.NewGuid(), "PPEIR-2026-05-0001", IssuanceReportType.PPEIR);

        asset.LifecycleState.ShouldBe(LifecycleState.TransferredOut);
        asset.CurrentAccountabilityId.ShouldBeNull();
        asset.CurrentCustodianId.ShouldBeNull();
        asset.CurrentLocationId.ShouldBeNull();
    }

    [Fact]
    public void MarkTransferredOut_WhenAlreadyTransferred_IsIdempotent()
    {
        var asset = NewAsset();
        var reportId = Guid.NewGuid();
        asset.MarkTransferredOut(reportId, "PPEIR-2026-05-0001", IssuanceReportType.PPEIR);

        Should.NotThrow(() =>
            asset.MarkTransferredOut(reportId, "PPEIR-2026-05-0001", IssuanceReportType.PPEIR));
        asset.LifecycleState.ShouldBe(LifecycleState.TransferredOut);
    }

    [Fact]
    public void MarkTransferredOut_FromUnserviceable_Throws()
    {
        var asset = NewAsset();
        asset.MarkUnserviceable(Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkTransferredOut(Guid.NewGuid(), "PPEIR-2026-05-0001", IssuanceReportType.PPEIR));
    }

    [Fact]
    public void MarkTransferredOut_FromDisposed_Throws()
    {
        var asset = NewAsset();
        asset.MarkUnserviceable(Guid.NewGuid());
        asset.Dispose(Guid.NewGuid(), DisposalMethod.Sale);

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkTransferredOut(Guid.NewGuid(), "PPEIR-2026-05-0001", IssuanceReportType.PPEIR));
    }

    private static AssetRegistry NewAsset()
    {
        var catalog = PropertyItemCatalog.Create(
            tenantId: "root",
            code: "DESK-001",
            description: "Office Desk",
            defaultPropertyClass: "07-PPE",
            defaultCategoryCode: "DSK",
            defaultUnit: "pc",
            uacsObjectCode: "10405030",
            estimatedUsefulLifeYears: 10);

        var propertyNo = PropertyNumber.Create("2026-NFA-00B-07-DSK-001");

        return AssetRegistry.Register(
            tenantId: "root",
            catalog: catalog,
            assetType: AssetType.PPE,
            category: AssetCategory.PPE,
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
}
