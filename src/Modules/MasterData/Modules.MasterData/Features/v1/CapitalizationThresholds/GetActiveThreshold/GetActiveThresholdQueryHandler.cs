using AMIS.Framework.Caching;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.GetActiveThreshold;

public sealed class GetActiveThresholdQueryHandler(MasterDataDbContext db, ICacheService cache)
    : IQueryHandler<GetActiveCapitalizationThresholdQuery, CapitalizationThresholdDto?>
{
    public async ValueTask<CapitalizationThresholdDto?> Handle(
        GetActiveCapitalizationThresholdQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = CapitalizationThresholdCache.ActiveKey(db.TenantInfo?.Identifier);

        var cached = await cache.GetItemAsync<CapitalizationThresholdDto>(cacheKey, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null) return cached;

        var threshold = await db.CapitalizationThresholds
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (threshold is null) return null;

        var dto = new CapitalizationThresholdDto(
            threshold.Id,
            threshold.CircularName,
            threshold.Description,
            threshold.CapitalizationAmount,
            threshold.SemiExpendableLowValueThreshold,
            threshold.EffectivityDate,
            threshold.IsActive);

        await cache.SetItemAsync(cacheKey, dto, CapitalizationThresholdCache.ActiveTtl, cancellationToken)
            .ConfigureAwait(false);

        return dto;
    }
}

