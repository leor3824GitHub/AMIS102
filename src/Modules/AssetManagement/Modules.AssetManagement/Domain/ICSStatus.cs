namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Lifecycle status of an Inventory Custodian Slip (ICS).
/// </summary>
public enum ICSStatus
{
    /// <summary>ICS is active and the custodian is currently accountable.</summary>
    Active = 0,

    /// <summary>
    /// ICS has been renewed. A new ICS (RenewedByICSId) was issued for the same custodian
    /// covering the same properties. This ICS is no longer the active accountability document.
    /// </summary>
    Renewed = 1,

    /// <summary>
    /// All items on this ICS were returned by the custodian via a Receipt of Returned
    /// Semi-Expendable Property (RRSP). The CancelledByRRSPId references that RRSP.
    /// </summary>
    CancelledByReturn = 2,

    /// <summary>
    /// Low-valued ICS only. The estimated useful life of all items has elapsed;
    /// accountability is automatically extinguished per COA Circular 2022-004 Section 4.10.
    /// </summary>
    Expired = 3,
}

