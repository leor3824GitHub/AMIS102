using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.GetCapitalizationThresholds;

public sealed class GetCapitalizationThresholdsQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetCapitalizationThresholdsQuery, IReadOnlyList<CapitalizationThresholdDto>>
{
    public async ValueTask<IReadOnlyList<CapitalizationThresholdDto>> Handle(
        GetCapitalizationThresholdsQuery query, CancellationToken cancellationToken)
    {
        return await db.CapitalizationThresholds
            .OrderByDescending(x => x.EffectivityDate)
            .Select(x => new CapitalizationThresholdDto(
                x.Id,
                x.CircularName,
                x.Description,
                x.CapitalizationAmount,
                x.SemiExpendableLowValueThreshold,
                x.EffectivityDate,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
