using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.CreateCapitalizationThreshold;

public sealed class CreateCapitalizationThresholdCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateCapitalizationThresholdCommand, Guid>
{
    public async ValueTask<Guid> Handle(
        CreateCapitalizationThresholdCommand command, CancellationToken cancellationToken)
    {
        var threshold = CapitalizationThreshold.Create(
            command.CircularName,
            command.Description,
            command.CapitalizationAmount,
            command.SemiExpendableLowValueThreshold,
            command.EffectivityDate);

        threshold.CreatedBy = currentUser.GetUserId().ToString();
        db.CapitalizationThresholds.Add(threshold);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return threshold.Id;
    }
}

