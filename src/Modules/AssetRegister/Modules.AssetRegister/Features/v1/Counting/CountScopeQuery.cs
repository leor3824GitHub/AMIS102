using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Domain.Assets;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting;

internal static class CountScopeQuery
{
    /// <summary>
    /// In-scope, on-the-books assets for a count session: same fund cluster, still in the active
    /// balance (Available or Assigned), and matching the session scope (PPE / SE / Both). Shared by
    /// the reconciliation report (to surface Uncounted rows) and the close handler (to flag those
    /// uncounted assets missing), so the two never diverge — COA Circular 2020-006 §6.3.1.d.
    /// </summary>
    public static IQueryable<AssetRegistry> InCountScope(
        this IQueryable<AssetRegistry> assets, string fundCluster, PhysicalCountScope scope)
    {
        ArgumentNullException.ThrowIfNull(assets);
        var q = assets.Where(a => a.FundCluster == fundCluster
            && (a.LifecycleState == LifecycleState.Available || a.LifecycleState == LifecycleState.Assigned));
        return scope switch
        {
            PhysicalCountScope.PPEOnly => q.Where(a => a.AssetType == AssetType.PPE),
            PhysicalCountScope.SEOnly => q.Where(a => a.AssetType == AssetType.SE),
            _ => q
        };
    }
}
