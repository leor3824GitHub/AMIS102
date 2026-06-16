using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.DeleteAccountability;

public sealed class DeleteAccountabilityCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<DeleteAccountabilityCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        // Only a still-pending document may be hard-deleted (accepted docs are voided via Cancel).
        accountability.EnsureDeletableDraft();

        // Release the reserved assets back to Available before removing the document.
        var assetIds = accountability.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);
        foreach (var asset in assets)
        {
            if (asset.LifecycleState == Contracts.v1.LifecycleState.Assigned)
                asset.ReturnToAvailable();
        }

        db.PropertyAccountabilities.Remove(accountability);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
