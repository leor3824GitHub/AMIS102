namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Lifecycle status of a tracked semi-expendable property unit.
/// </summary>
public enum PropertyStatus
{
    /// <summary>In supply custody, not yet issued.</summary>
    OnHand = 0,

    /// <summary>Issued to an end-user via ICS.</summary>
    Issued = 1,

    /// <summary>Returned by end-user; back in supply custody.</summary>
    Returned = 2,

    /// <summary>Transferred to another accountable officer via ITR.</summary>
    Transferred = 3,

    /// <summary>Disposed via IIRUSP (unserviceable).</summary>
    Disposed = 4,

    /// <summary>Reported lost, stolen, damaged, or destroyed via RLSDDSP.</summary>
    LostStolenDamaged = 5,
}
