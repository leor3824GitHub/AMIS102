using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetCondition;

public sealed class UpdateAssetConditionCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdateAssetConditionCommand, AssetRegistryDto>
{
    public async ValueTask<AssetRegistryDto> Handle(UpdateAssetConditionCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        asset.UpdateCondition(cmd.Condition);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return AssetRegistryMapper.ToDto(asset);
    }
}

