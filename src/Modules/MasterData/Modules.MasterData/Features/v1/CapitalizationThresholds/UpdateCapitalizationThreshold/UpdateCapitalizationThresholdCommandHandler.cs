using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.UpdateCapitalizationThreshold;

public sealed class UpdateCapitalizationThresholdCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateCapitalizationThresholdCommand, CapitalizationThresholdDto>
{
    public async ValueTask<CapitalizationThresholdDto> Handle(
        UpdateCapitalizationThresholdCommand command, CancellationToken cancellationToken)
    {
        var threshold = await db.CapitalizationThresholds
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Capitalization threshold {command.Id} not found.");

        threshold.Update(
            command.CircularName,
            command.Description,
            command.CapitalizationAmount,
            command.SemiExpendableLowValueThreshold,
            command.EffectivityDate);

        threshold.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

