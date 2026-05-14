using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Assets;

public sealed class AssetRegistry : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    // identity & classification
    public PropertyNumber PropertyNo { get; private set; } = default!;
    public Guid ItemId { get; private set; }
    public AssetType AssetType { get; private set; }
    public AssetCategory Category { get; private set; }
    public string PropertyClass { get; private set; } = default!;
    public string CategoryCode { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string Unit { get; private set; } = default!;

    // accounting
    public string FundCluster { get; private set; } = default!;
    public string UacsObjectCode { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }
    public decimal UnitCost { get; private set; }
    public int EstimatedUsefulLifeYears { get; private set; }
    public decimal AccumulatedDepreciation { get; private set; }
    public decimal AccumulatedImpairmentLosses { get; private set; }
    public decimal CarryingAmount => UnitCost - AccumulatedDepreciation - AccumulatedImpairmentLosses;

    // lifecycle & assignment
    public LifecycleState LifecycleState { get; private set; }
    public AssetCondition CurrentCondition { get; private set; }
    public Guid? CurrentCustodianId { get; private set; }
    public Guid? CurrentLocationId { get; private set; }
    public Guid? CurrentAccountabilityId { get; private set; }

    // provenance
    public Guid? SourceIARId { get; private set; }
    public Guid? SourcePurchaseOrderId { get; private set; }

    // audit
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private AssetRegistry() { }

    public static AssetRegistry Register(
        string tenantId,
        PropertyItemCatalog catalog,
        AssetType assetType,
        AssetCategory category,
        PropertyNumber propertyNo,
        string description,
        string? serialNo,
        string? brand,
        string? model,
        string fundCluster,
        DateOnly acquisitionDate,
        decimal unitCost,
        Guid? sourceIARId,
        Guid? sourcePurchaseOrderId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(propertyNo);

        if (unitCost <= 0)
            throw new InvalidOperationException("UnitCost must be greater than zero.");
        if (string.IsNullOrWhiteSpace(catalog.UacsObjectCode))
            throw new InvalidOperationException("Catalog item must carry a UacsObjectCode before an asset can be registered against it.");

        var registry = new AssetRegistry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyNo = propertyNo,
            ItemId = catalog.Id,
            AssetType = assetType,
            Category = category,
            PropertyClass = catalog.DefaultPropertyClass,
            CategoryCode = catalog.DefaultCategoryCode,
            Description = description,
            SerialNo = serialNo,
            Brand = brand,
            Model = model,
            Unit = catalog.DefaultUnit,
            FundCluster = fundCluster,
            UacsObjectCode = catalog.UacsObjectCode!,
            AcquisitionDate = acquisitionDate,
            UnitCost = unitCost,
            EstimatedUsefulLifeYears = catalog.EstimatedUsefulLifeYears,
            AccumulatedDepreciation = 0m,
            AccumulatedImpairmentLosses = 0m,
            LifecycleState = LifecycleState.Available,
            CurrentCondition = AssetCondition.InGoodCondition,
            SourceIARId = sourceIARId,
            SourcePurchaseOrderId = sourcePurchaseOrderId,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        registry.AddDomainEvent(new AssetRegisteredEvent(registry.Id, propertyNo.Value, assetType, tenantId));
        return registry;
    }

    public void AssignTo(Guid accountabilityId, Guid custodianId, Guid locationId)
    {
        EnsureNotDisposed();
        if (LifecycleState != LifecycleState.Available)
            throw new InvalidOperationException($"Cannot assign asset from state '{LifecycleState}'. Must be Available.");

        LifecycleState = LifecycleState.Assigned;
        CurrentAccountabilityId = accountabilityId;
        CurrentCustodianId = custodianId;
        CurrentLocationId = locationId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetIssuedEvent(Id, accountabilityId, custodianId, TenantId));
    }

    public void Transfer(Guid newAccountabilityId, Guid newCustodianId, Guid newLocationId)
    {
        EnsureNotDisposed();
        if (LifecycleState != LifecycleState.Assigned)
            throw new InvalidOperationException($"Cannot transfer asset from state '{LifecycleState}'. Must be Assigned.");

        var previous = CurrentAccountabilityId ?? Guid.Empty;
        CurrentAccountabilityId = newAccountabilityId;
        CurrentCustodianId = newCustodianId;
        CurrentLocationId = newLocationId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetTransferredEvent(Id, previous, newAccountabilityId, TenantId));
    }

    public void ReturnToAvailable()
    {
        EnsureNotDisposed();
        if (LifecycleState != LifecycleState.Assigned)
            throw new InvalidOperationException($"Cannot return asset from state '{LifecycleState}'. Must be Assigned.");

        var accountabilityId = CurrentAccountabilityId ?? Guid.Empty;
        LifecycleState = LifecycleState.Available;
        CurrentAccountabilityId = null;
        CurrentCustodianId = null;
        CurrentLocationId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetReturnedEvent(Id, accountabilityId, TenantId));
    }

    public void MarkUnderInvestigation(Guid incidentReportId)
    {
        EnsureNotDisposed();
        if (LifecycleState is not (LifecycleState.Assigned or LifecycleState.Available))
            throw new InvalidOperationException($"Cannot open an incident from state '{LifecycleState}'.");

        LifecycleState = LifecycleState.UnderInvestigation;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetLostEvent(Id, incidentReportId, TenantId));
    }

    public void RecordFoundAtStation(Guid sessionId, Guid entryId)
    {
        EnsureNotDisposed();
        AddDomainEvent(new AssetFoundAtStationEvent(sessionId, entryId, TenantId));
    }

    public void MarkRecovered(Guid incidentReportId)
    {
        if (LifecycleState != LifecycleState.UnderInvestigation)
            throw new InvalidOperationException("Only assets under investigation may be recovered.");

        LifecycleState = LifecycleState.Available;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetRecoveredEvent(Id, incidentReportId, TenantId));
    }

    public void MarkUnserviceable(Guid reportId)
    {
        EnsureNotDisposed();
        if (LifecycleState == LifecycleState.Unserviceable)
            return;

        LifecycleState = LifecycleState.Unserviceable;
        CurrentCondition = AssetCondition.Unserviceable;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetUnserviceableEvent(Id, reportId, TenantId));
    }

    public void MarkTransferredOut(Guid issuanceReportId, string reportNo, IssuanceReportType reportType)
    {
        EnsureNotDisposed();
        if (LifecycleState is LifecycleState.Unserviceable or LifecycleState.UnderInvestigation)
            throw new InvalidOperationException(
                $"Cannot transfer out an asset from state '{LifecycleState}'. Resolve the incident or unserviceable status first.");
        if (LifecycleState == LifecycleState.TransferredOut)
            return;

        LifecycleState = LifecycleState.TransferredOut;
        CurrentAccountabilityId = null;
        CurrentCustodianId = null;
        CurrentLocationId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetTransferredOutEvent(Id, issuanceReportId, reportNo, reportType, TenantId));
    }

    public void Dispose(Guid reportId, DisposalMethod method)
    {
        EnsureNotDisposed();
        if (LifecycleState != LifecycleState.Unserviceable && LifecycleState != LifecycleState.UnderInvestigation)
            throw new InvalidOperationException(
                $"Asset may only be disposed from Unserviceable or UnderInvestigation. Current state: '{LifecycleState}'.");

        LifecycleState = LifecycleState.Disposed;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetDisposedEvent(Id, PropertyNo.Value, method, TenantId));
    }

    public void RecordImpairment(decimal amount, string reason)
    {
        EnsureNotDisposed();
        if (amount <= 0)
            throw new InvalidOperationException("Impairment amount must be positive.");
        _ = reason;

        AccumulatedImpairmentLosses += amount;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordDepreciation(decimal amount)
    {
        EnsureNotDisposed();
        if (AssetType != AssetType.PPE)
            throw new InvalidOperationException("Only PPE assets may carry accumulated depreciation.");
        if (amount <= 0)
            throw new InvalidOperationException("Depreciation amount must be positive.");

        AccumulatedDepreciation += amount;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateCondition(AssetCondition condition)
    {
        EnsureNotDisposed();
        CurrentCondition = condition;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public AssetSnapshot Snapshot() =>
        AssetSnapshot.Create(
            PropertyNo.Value,
            Description,
            AssetType,
            UnitCost,
            Unit,
            EstimatedUsefulLifeYears,
            AcquisitionDate,
            UacsObjectCode,
            SerialNo,
            Brand,
            Model);

    private void EnsureNotDisposed()
    {
        if (LifecycleState == LifecycleState.Disposed)
            throw new InvalidOperationException("Disposed assets are terminal and may not be mutated.");
    }
}

