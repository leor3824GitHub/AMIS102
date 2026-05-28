namespace AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

public static class CapitalizationThresholdExtensions
{
    /// <summary>True when unit cost meets the capitalization cutoff — booked as PPE (COA Circular 2022-004 §4.2).</summary>
    public static bool IsCapitalAsset(this CapitalizationThresholdDto threshold, decimal unitCost) =>
        unitCost >= threshold.CapitalizationAmount;

    /// <summary>
    /// True when a sub-capitalization item is high-valued rather than low-valued semi-expendable
    /// (COA Circular 2022-004 §4.8). Only meaningful once the item is known not to be a capital asset.
    /// </summary>
    public static bool IsHighValueSemiExpendable(this CapitalizationThresholdDto threshold, decimal unitCost) =>
        unitCost >= threshold.SemiExpendableLowValueThreshold;
}
