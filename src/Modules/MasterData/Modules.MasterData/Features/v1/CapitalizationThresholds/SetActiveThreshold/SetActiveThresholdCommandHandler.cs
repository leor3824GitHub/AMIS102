using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.SetActiveThreshold;

public sealed class SetActiveThresholdCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<SetActiveCapitalizationThresholdCommand>
{
    public async ValueTask<Unit> Handle(
        SetActiveCapitalizationThresholdCommand command, CancellationToken cancellationToken)
    {
        var target = await db.CapitalizationThresholds
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Capitalization threshold {command.Id} not found.");

        // Deactivate all others first
        var allThresholds = await db.CapitalizationThresholds
            .Where(x => x.IsActive && x.Id != command.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var t in allThresholds)
        {
            t.Deactivate();
            t.LastModifiedBy = currentUser.GetUserId().ToString();
        }

        target.Activate();
        target.LastModifiedBy = currentUser.GetUserId().ToString();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

