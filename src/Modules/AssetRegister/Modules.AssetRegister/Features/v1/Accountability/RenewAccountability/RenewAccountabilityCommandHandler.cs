using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Accountability;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Domain.Assets;
using FSH.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Accountability.RenewAccountability;

public sealed class RenewAccountabilityCommandHandler(
    AssetRegisterDbContext db,
    IAccountabilityNumberGenerator numbers)
    : ICommandHandler<RenewAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(RenewAccountabilityCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var existing = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        string newDocumentNo;
        if (existing.AccountabilityType == AccountabilityType.SE_ICS)
        {
            var category = await InferIcsCategory(
                existing.Lines.Select(l => l.AssetRegistryId).ToList(), db, ct).ConfigureAwait(false);
            newDocumentNo = await numbers.NextIcsAsync(category, cmd.NewIssuedOn, ct).ConfigureAwait(false);
        }
        else
        {
            newDocumentNo = await numbers.NextParAsync(cmd.NewIssuedOn, ct).ConfigureAwait(false);
        }

        var successor = existing.Renew(newDocumentNo, cmd.NewIssuedOn, cmd.NewExpiresOn);
        db.PropertyAccountabilities.Add(successor);

        // Re-point each asset's CurrentAccountabilityId to the successor.
        var assetIds = successor.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        foreach (var asset in assets)
        {
            asset.Transfer(
                successor.Id,
                successor.ReceivedBy.EmployeeId,
                asset.CurrentLocationId ?? Guid.Empty);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(successor);
    }

    private static async Task<AssetCategory> InferIcsCategory(List<Guid> assetIds, AssetRegisterDbContext db, CancellationToken ct)
    {
        var hasHigh = await db.AssetRegistries
            .AnyAsync(a => assetIds.Contains(a.Id) && a.Category == AssetCategory.HighValuedSemi, ct).ConfigureAwait(false);
        return hasHigh ? AssetCategory.HighValuedSemi : AssetCategory.LowValuedSemi;
    }
}
