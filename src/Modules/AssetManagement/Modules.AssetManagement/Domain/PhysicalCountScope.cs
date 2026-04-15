namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Defines which asset track is covered by a Physical Count Session.
/// </summary>
public enum PhysicalCountScope
{
    /// <summary>Only PPE items (above capitalization threshold) are counted.</summary>
    PPEOnly = 0,

    /// <summary>Only Semi-Expendable properties (below capitalization threshold) are counted.</summary>
    SemiExpendableOnly = 1,

    /// <summary>Both PPE and Semi-Expendable properties are counted in the same session.</summary>
    Both = 2,
}
