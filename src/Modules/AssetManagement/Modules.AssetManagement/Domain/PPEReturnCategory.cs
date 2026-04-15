namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Category of a returned PPE item in a Receipt for Returned Property (RRP).
/// Determines downstream action: Serviceable items are reissued; Junked items are disposed.
/// </summary>
public enum PPEReturnCategory
{
    /// <summary>Item is in good condition and can be reissued.</summary>
    Serviceable = 0,

    /// <summary>Item is unserviceable/junked; forwarded to Accounting for net book value determination.</summary>
    Junked = 1,
}
