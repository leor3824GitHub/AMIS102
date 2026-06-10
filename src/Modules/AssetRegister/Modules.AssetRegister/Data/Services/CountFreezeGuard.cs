using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>
/// Blocks asset movements while a frozen physical count session covers the asset's
/// fund cluster and scope. Sessions started before the freeze workflow (FrozenOnUtc null)
/// never block.
/// </summary>
internal sealed class CountFreezeGuard(AssetRegisterDbContext db) : ICountFreezeGuard
{
    public async Task EnsureMovementAllowedAsync(IReadOnlyCollection<AssetRegistry> assets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (assets.Count == 0) return;

        var frozenSessions = await db.PhysicalCountSessions.AsNoTracking()
            .Where(s => (s.Status == PhysicalCountStatus.Ongoing || s.Status == PhysicalCountStatus.Reconciled)
                        && s.FrozenOnUtc != null)
            .Select(s => new { s.Code, s.FundCluster, s.Scope })
            .ToListAsync(ct).ConfigureAwait(false);
        if (frozenSessions.Count == 0) return;

        foreach (var asset in assets)
        {
            var blocking = frozenSessions.FirstOrDefault(s =>
                string.Equals(s.FundCluster, asset.FundCluster, StringComparison.OrdinalIgnoreCase)
                && ScopeCovers(s.Scope, asset.AssetType));
            if (blocking is not null)
            {
                throw new CustomException(
                    $"Asset movements are blocked: physical count session '{blocking.Code}' has frozen " +
                    $"fund cluster '{blocking.FundCluster}' ({blocking.Scope}). " +
                    $"Asset '{asset.PropertyNo.Value}' cannot be moved until the count is closed.",
                    null,
                    HttpStatusCode.Conflict);
            }
        }
    }

    private static bool ScopeCovers(PhysicalCountScope scope, AssetType assetType) => scope switch
    {
        PhysicalCountScope.Both => true,
        PhysicalCountScope.PPEOnly => assetType == AssetType.PPE,
        PhysicalCountScope.SEOnly => assetType == AssetType.SE,
        _ => false
    };
}
