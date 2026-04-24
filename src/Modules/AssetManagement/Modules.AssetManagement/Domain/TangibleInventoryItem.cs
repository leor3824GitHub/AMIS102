using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Tangible Inventory document.
/// References a pre-registered TangibleItem (one-to-one: each item appears on exactly one report).
/// AssetType is computed at creation by comparing UnitCost against the active capitalization
/// threshold and stored as a snapshot for historical accuracy.
/// </summary>
public sealed class TangibleInventoryItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent TangibleInventory.</summary>
    public Guid TangibleInventoryId { get; private set; }

    /// <summary>FK to the pre-registered tangible item (unique — one item per report).</summary>
    public Guid TangibleItemId { get; private set; }

    /// <summary>Source document reference (e.g., PO number, DR number).</summary>
    public string? Reference { get; private set; }

    /// <summary>
    /// SE or PPE — snapshotted at creation by comparing UnitCost against
    /// the active CapitalizationThreshold.CapitalizationAmount.
    /// </summary>
    public AssetType AssetType { get; private set; }

    /// <summary>
    /// Capitalization amount from the active threshold at the time this item was received.
    /// Stored as a snapshot so the classification decision is auditable even after
    /// the threshold policy changes.
    /// </summary>
    public decimal ThresholdAmountUsed { get; private set; }

    /// <summary>
    /// True once a downstream accountability document (ICS for SE, PAR for PPE) has been
    /// created from this item.  Prevents double-issuance.
    /// </summary>
    public bool IsIssued { get; private set; }

    // --- Snapshot fields (copied from TangibleItem at receipt time) ---

    /// <summary>Snapshot: property number.</summary>
    public string PropertyNo { get; private set; } = default!;

    /// <summary>Snapshot: FK to the unified item catalog.</summary>
    public Guid ItemId { get; private set; }

    /// <summary>Snapshot: item description / name.</summary>
    public string? Description { get; private set; }

    /// <summary>Snapshot: acquisition date.</summary>
    public DateOnly AcquisitionDate { get; private set; }

    /// <summary>Snapshot: quantity.</summary>
    public int Quantity { get; private set; }

    /// <summary>Snapshot: unit cost.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Computed: Quantity × UnitCost. Stored for reporting.</summary>
    public decimal Amount { get; private set; }

    public static TangibleInventoryItem Create(
        Guid tangibleInventoryId,
        Guid tangibleItemId,
        string? reference,
        AssetType assetType,
        decimal thresholdAmountUsed,
        string propertyNo,
        Guid itemId,
        string? description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost)
    {
        return new TangibleInventoryItem
        {
            Id                   = Guid.NewGuid(),
            TangibleInventoryId  = tangibleInventoryId,
            TangibleItemId       = tangibleItemId,
            Reference            = reference,
            AssetType            = assetType,
            ThresholdAmountUsed  = thresholdAmountUsed,
            IsIssued             = false,
            PropertyNo           = propertyNo,
            ItemId               = itemId,
            Description          = description,
            AcquisitionDate      = acquisitionDate,
            Quantity             = quantity,
            UnitCost             = unitCost,
            Amount               = quantity * unitCost,
        };
    }

    /// <summary>
    /// Marks this item as issued.  Called when an ICS (SE) or PAR (PPE) is created
    /// referencing this inventory item.
    /// </summary>
    public void MarkIssued() => IsIssued = true;

    /// <summary>
    /// Marks this item as returned (no longer issued).  Called when an RRSP (SE) or
    /// RRP (PPE) is created returning this inventory item.  Allows the item to be
    /// re-issued via a new ICS/PAR.
    /// </summary>
    public void MarkReturned() => IsIssued = false;

    /// <summary>
    /// Reclassifies this inventory item when the capitalization threshold policy changes.
    /// Updates both AssetType and the snapshot ThresholdAmountUsed to reflect the new policy.
    /// </summary>
    public void Reclassify(AssetType newAssetType, decimal newThresholdAmount)
    {
        AssetType           = newAssetType;
        ThresholdAmountUsed = newThresholdAmount;
    }
}
