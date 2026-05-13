using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.GetActiveThreshold;

public sealed class GetActiveThresholdQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetActiveCapitalizationThresholdQuery, CapitalizationThresholdDto?>
{
    public async ValueTask<CapitalizationThresholdDto?> Handle(
        GetActiveCapitalizationThresholdQuery query, CancellationToken cancellationToken)
    {
        var threshold = await db.CapitalizationThresholds
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (threshold is null) return null;

        return new CapitalizationThresholdDto(
            threshold.Id,
            threshold.CircularName,
            threshold.Description,
            threshold.CapitalizationAmount,
            threshold.SemiExpendableLowValueThreshold,
            threshold.EffectivityDate,
            threshold.IsActive);
    }
}

