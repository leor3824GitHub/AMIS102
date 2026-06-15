using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.CancelAccountability;

public sealed class CancelAccountabilityCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<CancelAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(CancelAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        accountability.Cancel(cmd.Reason);

        // Free the assets so they're back to Available.
        var assetIds = accountability.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);
        foreach (var asset in assets)
        {
            if (asset.LifecycleState == Contracts.v1.LifecycleState.Assigned)
                asset.ReturnToAvailable();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}

