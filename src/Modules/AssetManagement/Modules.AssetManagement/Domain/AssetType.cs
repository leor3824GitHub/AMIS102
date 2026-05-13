namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Determines whether a received tangible item is classified as
/// Semi-Expendable (SE) or Property, Plant and Equipment (PPE).
/// Snapshotted onto TangibleInventoryItem at creation time by comparing
/// UnitCost against the active CapitalizationThreshold.CapitalizationAmount.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Unit cost is below the capitalization threshold.
    /// Downstream chain: TangibleInventoryItem → ICS → SMIR.
    /// </summary>
    SE = 0,

    /// <summary>
    /// Unit cost meets or exceeds the capitalization threshold.
    /// Downstream chain: TangibleInventoryItem → PAR → PPEIR.
    /// </summary>
    PPE = 1,
}

