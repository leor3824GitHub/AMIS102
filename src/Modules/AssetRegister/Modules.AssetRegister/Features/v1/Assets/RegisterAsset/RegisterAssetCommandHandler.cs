using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.RegisterAsset;

public sealed class RegisterAssetCommandHandler(
    AssetRegisterDbContext db,
    IPropertyNumberGenerator propertyNumbers)
    : ICommandHandler<RegisterAssetCommand, AssetRegistryDto>
{
    public async ValueTask<AssetRegistryDto> Handle(RegisterAssetCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var catalog = await db.PropertyItemCatalogs.FirstOrDefaultAsync(c => c.Id == cmd.CatalogItemId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.CatalogItemId}' not found.");
        if (!catalog.IsActive)
            throw new InvalidOperationException("Cannot register an asset against a deactivated catalog item.");

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var propertyNo = await propertyNumbers.NextAsync(
            cmd.AssetType, cmd.SubMajorAccount, cmd.GeneralLedgerAccount, cmd.LocationCode, cmd.AcquisitionDate, ct)
            .ConfigureAwait(false);

        var asset = AssetRegistry.Register(
            tenantId,
            catalog,
            cmd.AssetType,
            cmd.Category,
            propertyNo,
            cmd.Description,
            cmd.SerialNo,
            cmd.Brand,
            cmd.Model,
            cmd.FundCluster,
            cmd.AcquisitionDate,
            cmd.UnitCost,
            cmd.SourceIARId,
            cmd.SourcePurchaseOrderId);

        db.AssetRegistries.Add(asset);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return AssetRegistryMapper.ToDto(asset);
    }
}

