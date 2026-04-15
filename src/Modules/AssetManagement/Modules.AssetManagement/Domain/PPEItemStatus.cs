namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Lifecycle status of a PPE (Property, Plant and Equipment) item.
/// </summary>
public enum PPEItemStatus
{
    /// <summary>In supply custody, not yet assigned to an accountable officer.</summary>
    OnHand = 0,

    /// <summary>Assigned to an accountable officer via PAR.</summary>
    IssuedPAR = 1,

    /// <summary>Transferred to another office/department via PPEIR.</summary>
    Transferred = 2,

    /// <summary>Returned by accountable officer via RRP (serviceable); back in supply custody.</summary>
    Returned = 3,

    /// <summary>Disposed/junked via RRP (junked) or unserviceable process.</summary>
    Disposed = 4,
}
