using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A single asset verification record within a Physical Count Session.
/// Covers both PPE items and Semi-Expendable Properties in the same table.
/// Generated from the asset registry at session creation; updated during the walk-through.
/// Per COA Circular 2020-006 — Inventory Count Form (ICF) line item.
/// </summary>
public sealed class PhysicalCountEntry : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent Physical Count Session.</summary>
    public Guid SessionId { get; private set; }

    // ── Asset reference ───────────────────────────────────────────────────────

    /// <summary>
    /// FK to TangibleInventoryItem. Null for "Found at Station" entries
    /// where there is no pre-generated checklist row.
    /// </summary>
    public Guid? TangibleInventoryItemId { get; private set; }

    // ── Snapshot fields (frozen from the asset at session creation) ───────────

    /// <summary>Property number / property code snapshot.</summary>
    public string PropertyNumber { get; private set; } = default!;

    /// <summary>Asset description snapshot.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Unit cost snapshot at the time the session was created.</summary>
    public decimal UnitCost { get; private set; }

    // ── Walk-through results (filled during physical count) ───────────────────

    /// <summary>
    /// Outcome recorded by the inventory team.
    /// Null until the entry is verified or explicitly marked NotFound.
    /// </summary>
    public PhysicalCountEntryResult? Result { get; private set; }

    /// <summary>
    /// Physical condition observed during the count.
    /// Required when Result is Found or FoundAtStation.
    /// </summary>
    public PhysicalCountCondition? Condition { get; private set; }

    /// <summary>
    /// Remarks entered by the inventory team (e.g., "sticker missing", "found in storeroom").
    /// </summary>
    public string? Remarks { get; private set; }

    /// <summary>
    /// Quantity counted (defaults to 1 for tracked assets; may be > 1 for bulk semi-expendable).
    /// </summary>
    public int QuantityOnHand { get; private set; } = 1;

    /// <summary>
    /// When set, indicates this entry was verified via camera scan (property sticker photo).
    /// Stores the UTC timestamp of the scan event.
    /// </summary>
    public DateTimeOffset? ScannedOnUtc { get; private set; }

    /// <summary>
    /// Optional: file path or object-storage key of the sticker/asset photo captured during scan.
    /// </summary>
    public string? PhotoPath { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a pre-populated checklist entry from a known TangibleInventoryItem (PPE or SE).</summary>
    public static PhysicalCountEntry FromTangibleInventoryItem(
        string tenantId,
        Guid sessionId,
        Guid tangibleInventoryItemId,
        string propertyNumber,
        string description,
        decimal unitCost)
    {
        return new PhysicalCountEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            PropertyNumber = propertyNumber,
            Description = description,
            UnitCost = unitCost,
            QuantityOnHand = 1,
        };
    }

    /// <summary>
    /// Creates a "Found at Station" entry for an asset discovered during the count
    /// that is not on the pre-generated checklist (e.g., unrecorded or mis-assigned asset).
    /// </summary>
    public static PhysicalCountEntry CreateFoundAtStation(
        string tenantId,
        Guid sessionId,
        string propertyNumber,
        string description,
        decimal unitCost,
        PhysicalCountCondition condition,
        string? remarks,
        string? photoPath)
    {
        return new PhysicalCountEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            PropertyNumber = propertyNumber,
            Description = description,
            UnitCost = unitCost,
            Result = PhysicalCountEntryResult.FoundAtStation,
            Condition = condition,
            Remarks = remarks,
            QuantityOnHand = 1,
            PhotoPath = photoPath,
        };
    }

    /// <summary>Records that the asset was physically found and verified by the inventory team.</summary>
    public void MarkFound(PhysicalCountCondition condition, int quantityOnHand, string? remarks)
    {
        Result = PhysicalCountEntryResult.Found;
        Condition = condition;
        QuantityOnHand = quantityOnHand;
        Remarks = remarks;
    }

    /// <summary>Records that the asset was found via camera scan of its property sticker.</summary>
    public void MarkFoundViaScan(PhysicalCountCondition condition, int quantityOnHand, string? remarks, string? photoPath)
    {
        Result = PhysicalCountEntryResult.Found;
        Condition = condition;
        QuantityOnHand = quantityOnHand;
        Remarks = remarks;
        PhotoPath = photoPath;
        ScannedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Records that the asset was not located during the walk-through.</summary>
    public void MarkNotFound(string? remarks)
    {
        Result = PhysicalCountEntryResult.NotFound;
        Condition = null;
        Remarks = remarks;
    }
}
