using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.CreateCapitalizationThreshold;

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
