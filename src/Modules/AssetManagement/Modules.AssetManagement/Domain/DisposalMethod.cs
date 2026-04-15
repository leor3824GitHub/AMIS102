namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Method of disposal used in the Inspection and Inventory Report of
/// Unserviceable Semi-Expendable Property (IIRUSP).
/// Per COA Circular 2022-004 and relevant DBM circulars.
/// </summary>
public enum DisposalMethod
{
    /// <summary>Sold via public bidding or negotiated sale.</summary>
    Sale = 0,

    /// <summary>Physically destroyed (shredding, burning, etc.).</summary>
    Destruction = 1,

    /// <summary>Donated to another government entity or charitable institution.</summary>
    Donation = 2,

    /// <summary>Transferred to another government agency.</summary>
    Transfer = 3,

    /// <summary>Other disposal method not covered above.</summary>
    Others = 4,
}
