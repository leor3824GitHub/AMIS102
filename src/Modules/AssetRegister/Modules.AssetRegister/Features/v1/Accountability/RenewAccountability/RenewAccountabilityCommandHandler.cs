using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.RenewAccountability;

public sealed class RenewAccountabilityCommandHandler(
    AssetRegisterDbContext db,
    IAccountabilityNumberGenerator numbers,
    ICountFreezeGuard freezeGuard)
    : ICommandHandler<RenewAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(RenewAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var existing = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        string newDocumentNo;
        if (existing.AccountabilityType == AccountabilityType.SE_ICS)
        {
            var category = await InferIcsCategory(
                existing.Lines.Select(l => l.AssetRegistryId).ToList(), db, cancellationToken).ConfigureAwait(false);
            newDocumentNo = await numbers.NextIcsAsync(category, cmd.NewIssuedOn, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            newDocumentNo = await numbers.NextParAsync(cmd.NewIssuedOn, cancellationToken).ConfigureAwait(false);
        }

        var successor = existing.Renew(newDocumentNo, cmd.NewIssuedOn, cmd.NewExpiresOn);
        db.PropertyAccountabilities.Add(successor);

        // Re-point each asset's CurrentAccountabilityId to the successor.
        var assetIds = successor.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);
        foreach (var asset in assets)
        {
            asset.Transfer(
                successor.Id,
                successor.ReceivedBy.EmployeeId,
                asset.CurrentLocationId ?? Guid.Empty);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(successor);
    }

    private static async Task<AssetCategory> InferIcsCategory(List<Guid> assetIds, AssetRegisterDbContext db, CancellationToken cancellationToken)
    {
        var hasHigh = await db.AssetRegistries
            .AnyAsync(a => assetIds.Contains(a.Id) && a.Category == AssetCategory.HighValuedSemi, cancellationToken).ConfigureAwait(false);
        return hasHigh ? AssetCategory.HighValuedSemi : AssetCategory.LowValuedSemi;
    }
}

