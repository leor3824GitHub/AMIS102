namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Observed physical condition of an asset during a Physical Count Session.
/// Per COA Circular 2020-006, Section 6.2.6 (ICF column requirements).
/// </summary>
public enum PhysicalCountCondition
{
    /// <summary>Asset is in good working condition.</summary>
    Good = 0,

    /// <summary>Asset is functional but requires repair.</summary>
    NeedsRepair = 1,

    /// <summary>Asset is no longer serviceable.</summary>
    Unserviceable = 2,

    /// <summary>Asset is obsolete.</summary>
    Obsolete = 3,

    /// <summary>Asset is serviceable but no longer needed by the office.</summary>
    NoLongerNeeded = 4,
}
