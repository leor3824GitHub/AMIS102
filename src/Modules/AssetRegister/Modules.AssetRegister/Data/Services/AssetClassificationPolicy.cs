using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>
/// Single source of truth for classifying a catalog-backed asset as PPE vs Semi-Expendable
/// (and the SE sub-category) from its catalog property class and unit cost. Per COA Circular
/// 2022-004: §4.2 PPE capitalization cutoff (₱50,000), §4.8 high- vs low-value SE (₱5,000).
/// Reused when materializing assets from accepted IARs and from physical-count "found at
/// station" entries so the two paths classify identically.
/// </summary>
internal static class AssetClassificationPolicy
{
    /// <summary>Fallback thresholds when no active CapitalizationThreshold master-data row exists.</summary>
    public static readonly CapitalizationThresholdDto FallbackThreshold = new(
        Guid.Empty, "COA Circular 2022-004 (default)", string.Empty,
        CapitalizationAmount: 50_000m, SemiExpendableLowValueThreshold: 5_000m,
        EffectivityDate: default, IsActive: true);

    /// <summary>
    /// A catalog item tagged PPE in its PropertyClass is booked as PPE even below the cutoff
    /// (COA classification wins); otherwise the unit cost decides PPE vs high-/low-value SE.
    /// </summary>
    public static (AssetType AssetType, AssetCategory Category) Classify(
        PropertyItemCatalog catalog, decimal unitCost, CapitalizationThresholdDto threshold)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(threshold);

        var isPpeClass = catalog.DefaultPropertyClass.Contains("PPE", StringComparison.OrdinalIgnoreCase);
        if (isPpeClass || threshold.IsCapitalAsset(unitCost))
            return (AssetType.PPE, AssetCategory.PPE);

        return threshold.IsHighValueSemiExpendable(unitCost)
            ? (AssetType.SE, AssetCategory.HighValuedSemi)
            : (AssetType.SE, AssetCategory.LowValuedSemi);
    }
}
