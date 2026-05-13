using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Current-state record for a physical asset unit.
/// Ledger documents remain immutable history; this aggregate stores the latest state.
/// </summary>
public sealed class AssetRegistry : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid TangibleInventoryItemId { get; private set; }
    public Guid ItemId { get; private set; }
    public string PropertyNo { get; private set; } = default!;
    public AssetType AssetType { get; private set; }
    public DateOnly AcquisitionDate { get; private set; }
    public decimal UnitCost { get; private set; }

    public AssetLifecycleState LifecycleState { get; private set; } = AssetLifecycleState.PendingReceipt;
    public PropertyStatus? CurrentPropertyStatus { get; private set; }
    public Guid? CurrentCustodianId { get; private set; }
    public Guid? CurrentLocationId { get; private set; }
    public Guid? CurrentAssignmentHistoryId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static AssetRegistry Create(
        string tenantId,
        Guid tangibleInventoryItemId,
        Guid itemId,
        string propertyNo,
        AssetType assetType,
        DateOnly acquisitionDate,
        decimal unitCost)
    {
        return new AssetRegistry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemId = itemId,
            PropertyNo = propertyNo,
            AssetType = assetType,
            AcquisitionDate = acquisitionDate,
            UnitCost = unitCost,
            LifecycleState = AssetLifecycleState.Available,
            CurrentPropertyStatus = PropertyStatus.OnHand,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void AssignTo(Guid custodianId, Guid? locationId)
    {
        CurrentCustodianId = custodianId;
        CurrentLocationId = locationId;
        LifecycleState = AssetLifecycleState.Assigned;
        CurrentPropertyStatus = PropertyStatus.Issued;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ReturnToStock(Guid? locationId = null)
    {
        CurrentCustodianId = null;
        CurrentLocationId = locationId;
        LifecycleState = AssetLifecycleState.Available;
        CurrentPropertyStatus = PropertyStatus.Returned;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void TransferOut()
    {
        CurrentCustodianId = null;
        LifecycleState = AssetLifecycleState.InTransit;
        CurrentPropertyStatus = PropertyStatus.Transferred;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkDisposed()
    {
        CurrentCustodianId = null;
        LifecycleState = AssetLifecycleState.Disposed;
        CurrentPropertyStatus = PropertyStatus.Disposed;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkUnderInvestigation()
    {
        LifecycleState = AssetLifecycleState.UnderInvestigation;
        CurrentPropertyStatus = PropertyStatus.LostStolenDamaged;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ReclassifyAssetType(AssetType assetType)
    {
        AssetType = assetType;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void LinkCurrentAssignment(Guid assignmentHistoryId)
    {
        CurrentAssignmentHistoryId = assignmentHistoryId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

