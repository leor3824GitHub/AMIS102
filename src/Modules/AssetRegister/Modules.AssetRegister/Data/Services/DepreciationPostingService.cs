using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using AMIS.Modules.AssetRegister.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Data.Services;

/// <summary>
/// Posts COA GAM straight-line depreciation for PPE assets. Walks each asset month-by-month from
/// where it was last posted up to the requested period, appending one <see cref="DepreciationEntry"/>
/// per month (the PPELC source) and advancing the asset's accumulated depreciation. Idempotent: the
/// per-asset cursor starts after <c>DepreciatedThrough</c>, and the unique (asset, period) index is a
/// backstop. SE assets and fully-depreciated/disposed PPE are skipped.
/// </summary>
public sealed class DepreciationPostingService(AssetRegisterDbContext db)
{
    /// <summary>
    /// Tenant-scoped run: respects the ambient multi-tenant filter. Invoked both by the HTTP endpoint
    /// and, per tenant, by the monthly recurring job (which sets the tenant context before each call).
    /// </summary>
    public Task<RunDepreciationResultDto> PostThroughAsync(DateOnly asOfPeriod, CancellationToken ct)
        => PostCoreAsync(db.AssetRegistries, asOfPeriod, ct);

    private async Task<RunDepreciationResultDto> PostCoreAsync(
        IQueryable<AssetRegistry> source, DateOnly asOfPeriod, CancellationToken ct)
    {
        var periodEnd = FirstOfMonth(asOfPeriod);

        var assets = await source
            .Where(a => a.AssetType == AssetType.PPE
                     && a.LifecycleState != LifecycleState.Disposed
                     && a.DepreciationStartDate <= periodEnd)
            .ToListAsync(ct).ConfigureAwait(false);

        var entriesPosted = 0;
        var assetsProcessed = 0;
        var totalCharged = 0m;

        foreach (var asset in assets)
        {
            if (asset.IsFullyDepreciated)
                continue;

            var monthly = asset.MonthlyDepreciation();
            if (monthly <= 0m)
                continue;

            var cursor = asset.DepreciatedThrough is null
                ? FirstOfMonth(asset.DepreciationStartDate)
                : FirstOfMonth(asset.DepreciatedThrough.Value).AddMonths(1);

            var touched = false;
            while (cursor <= periodEnd && !asset.IsFullyDepreciated)
            {
                var amount = asset.PostDepreciation(cursor, monthly);
                db.DepreciationEntries.Add(DepreciationEntry.Create(
                    asset.TenantId, asset.Id, cursor, amount,
                    asset.AccumulatedDepreciation, asset.CarryingAmount));

                entriesPosted++;
                totalCharged += amount;
                touched = true;
                cursor = cursor.AddMonths(1);
            }

            if (touched)
                assetsProcessed++;
        }

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException) when (entriesPosted > 0)
        {
            // A concurrent run posted one or more of these (tenant, asset, period) rows first —
            // the unique index rejects the duplicate. Depreciation is idempotent and self-healing,
            // so treat this run as superseded: nothing from it is committed (the failed SaveChanges
            // rolls back the whole batch) and the winning run did the work. Any month this run would
            // have added is caught up on the next run. The context is discarded by the caller's scope,
            // so the uncommitted in-memory mutations never persist.
            return new RunDepreciationResultDto(periodEnd, 0, 0, 0m);
        }

        return new RunDepreciationResultDto(periodEnd, assetsProcessed, entriesPosted, totalCharged);
    }

    private static DateOnly FirstOfMonth(DateOnly d) => new(d.Year, d.Month, 1);
}
