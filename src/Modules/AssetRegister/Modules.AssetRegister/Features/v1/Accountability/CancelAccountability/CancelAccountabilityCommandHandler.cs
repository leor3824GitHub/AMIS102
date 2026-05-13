using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.CancelAccountability;

public sealed class CancelAccountabilityCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CancelAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(CancelAccountabilityCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        accountability.Cancel(cmd.Reason);

        // Free the assets so they're back to Available.
        var assetIds = accountability.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        foreach (var asset in assets)
        {
            if (asset.LifecycleState == Contracts.v1.LifecycleState.Assigned)
                asset.ReturnToAvailable();
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}

