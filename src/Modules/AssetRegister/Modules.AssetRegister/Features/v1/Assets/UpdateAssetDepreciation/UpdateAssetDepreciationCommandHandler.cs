using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetDepreciation;

public sealed class UpdateAssetDepreciationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdateAssetDepreciationCommand, AssetRegistryDto>
{
    public async ValueTask<AssetRegistryDto> Handle(UpdateAssetDepreciationCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        asset.UpdateDepreciation(cmd.ResidualValue, cmd.EstimatedUsefulLifeYears, cmd.DepreciationMethod);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AssetRegistryMapper.ToDto(asset);
    }
}
