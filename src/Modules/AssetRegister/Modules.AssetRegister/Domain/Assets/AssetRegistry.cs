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

    // Depreciation schedule (PPE only; COA GAM straight-line). Policy (residual %, method) is
    // inherited from the catalog at registration and may be overridden per asset via UpdateDepreciation.
    // SE is expensed on issue and never depreciated: ResidualValue stays 0 and these fields are inert.
    public decimal ResidualValue { get; private set; }
    public DepreciationMethod DepreciationMethod { get; private set; }
    public DateOnly DepreciationStartDate { get; private set; }
    public DateOnly? DepreciatedThrough { get; private set; }

    /// <summary>True once a PPE asset's carrying amount has reached its residual value (fully depreciated).</summary>
    public bool IsFullyDepreciated => AssetType == AssetType.PPE && CarryingAmount <= ResidualValue;

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

        // Depreciation policy is inherited from the catalog (residual % defaults to COA's 5%).
        // Depreciation starts the month after acquisition. SE never depreciates.
        var residualValue = assetType == AssetType.PPE
            ? Math.Round(unitCost * (catalog.ResidualValuePercent / 100m), 2, MidpointRounding.AwayFromZero)
            : 0m;
        var depreciationStart = assetType == AssetType.PPE
            ? new DateOnly(acquisitionDate.Year, acquisitionDate.Month, 1).AddMonths(1)
            : acquisitionDate;

        var registry = new AssetRegistry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PropertyNo = propertyNo,
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
            ResidualValue = residualValue,
            DepreciationMethod = catalog.DepreciationMethod,
            DepreciationStartDate = depreciationStart,
            DepreciatedThrough = null,
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
        // UnderInvestigation is allowed so a formal RLSDDSP can be filed against an asset that a
        // physical count already flagged missing (see MarkMissingFromCount) — the call is then
        // idempotent on state but still links the incident.
        if (LifecycleState is not (LifecycleState.Assigned or LifecycleState.Available or LifecycleState.UnderInvestigation))
            throw new InvalidOperationException($"Cannot open an incident from state '{LifecycleState}'.");

        LifecycleState = LifecycleState.UnderInvestigation;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetLostEvent(Id, incidentReportId, TenantId));
    }

    /// <summary>
    /// Drops a not-found asset out of the active inventory balance at physical-count close: an
    /// asset still on the books (Available/Assigned) that the committee recorded Missing or never
    /// counted moves to UnderInvestigation, routing it into the RLSDDSP / relief-of-accountability
    /// process. No-op for assets already outside the active balance (disposed/unserviceable/
    /// transferred/under investigation), so re-running close is safe.
    /// </summary>
    public void MarkMissingFromCount(Guid physicalCountSessionId)
    {
        if (LifecycleState is not (LifecycleState.Available or LifecycleState.Assigned))
            return;

        LifecycleState = LifecycleState.UnderInvestigation;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AssetReportedMissingFromCountEvent(Id, physicalCountSessionId, TenantId));
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

    /// <summary>
    /// COA GAM straight-line monthly charge, computed prospectively: the REMAINING depreciable amount
    /// (CarryingAmount − ResidualValue) spread over the REMAINING months of useful life. For a fresh
    /// asset this equals (Cost − ResidualValue) / (UsefulLifeYears × 12); after a change-in-estimate via
    /// <see cref="UpdateDepreciation"/> it re-spreads the unrecovered cost over the revised remaining life.
    /// Returns 0 for SE, when fully depreciated, or when no useful life remains.
    /// </summary>
    public decimal MonthlyDepreciation()
    {
        if (AssetType != AssetType.PPE || EstimatedUsefulLifeYears <= 0)
            return 0m;

        var remainingMonths = (EstimatedUsefulLifeYears * 12) - ElapsedDepreciationMonths();
        if (remainingMonths <= 0)
            return 0m;

        var remainingDepreciable = CarryingAmount - ResidualValue;
        if (remainingDepreciable <= 0m)
            return 0m;

        return DepreciationMethod switch
        {
            DepreciationMethod.StraightLine =>
                Math.Round(remainingDepreciable / remainingMonths, 2, MidpointRounding.AwayFromZero),
            _ => throw new InvalidOperationException($"Depreciation method '{DepreciationMethod}' is not supported."),
        };
    }

    /// <summary>Number of months already depreciated, inclusive of the through-month; 0 if none posted.</summary>
    private int ElapsedDepreciationMonths()
    {
        if (DepreciatedThrough is null)
            return 0;
        var months = ((DepreciatedThrough.Value.Year - DepreciationStartDate.Year) * 12)
                     + (DepreciatedThrough.Value.Month - DepreciationStartDate.Month) + 1;
        return months < 0 ? 0 : months;
    }

    /// <summary>
    /// Overrides this asset's depreciation parameters (residual value, useful life, method) — the
    /// per-asset exception to the catalog policy. Applied prospectively: already-posted accumulated
    /// depreciation is untouched; future monthly charges and the residual floor use the new values.
    /// </summary>
    public void UpdateDepreciation(decimal residualValue, int estimatedUsefulLifeYears, DepreciationMethod method)
    {
        EnsureNotDisposed();
        if (AssetType != AssetType.PPE)
            throw new InvalidOperationException("Only PPE assets carry depreciation parameters.");
        if (residualValue < 0m)
            throw new InvalidOperationException("ResidualValue cannot be negative.");
        if (residualValue >= UnitCost)
            throw new InvalidOperationException("ResidualValue must be less than the acquisition cost.");
        if (estimatedUsefulLifeYears <= 0)
            throw new InvalidOperationException("EstimatedUsefulLifeYears must be greater than zero.");

        ResidualValue = residualValue;
        EstimatedUsefulLifeYears = estimatedUsefulLifeYears;
        DepreciationMethod = method;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Posts one month's depreciation for <paramref name="period"/> (first day of the month),
    /// capped so the carrying amount never falls below <see cref="ResidualValue"/>. Advances
    /// <see cref="DepreciatedThrough"/> and returns the amount actually charged (0 once fully
    /// depreciated). Rejects periods already posted, keeping catch-up runs idempotent.
    /// </summary>
    public decimal PostDepreciation(DateOnly period, decimal scheduledAmount)
    {
        EnsureNotDisposed();
        if (AssetType != AssetType.PPE)
            throw new InvalidOperationException("Only PPE assets may be depreciated.");
        if (scheduledAmount < 0)
            throw new InvalidOperationException("Scheduled depreciation amount cannot be negative.");
        if (DepreciatedThrough is not null && period <= DepreciatedThrough.Value)
            throw new InvalidOperationException(
                $"Depreciation for period {period:yyyy-MM} on asset '{PropertyNo.Value}' was already posted.");

        var remainingDepreciable = CarryingAmount - ResidualValue;
        var amount = remainingDepreciable <= 0m ? 0m : Math.Min(scheduledAmount, remainingDepreciable);

        if (amount > 0m)
            AccumulatedDepreciation += amount;

        DepreciatedThrough = period;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return amount;
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
            Model,
            CarryingAmount,
            PropertyClass,
            CategoryCode);

    private void EnsureNotDisposed()
    {
        if (LifecycleState == LifecycleState.Disposed)
            throw new InvalidOperationException("Disposed assets are terminal and may not be mutated.");
    }
}

