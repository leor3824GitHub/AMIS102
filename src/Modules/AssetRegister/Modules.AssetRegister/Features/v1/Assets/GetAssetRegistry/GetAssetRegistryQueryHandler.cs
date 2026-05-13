using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetRegistry;

public sealed class GetAssetRegistryQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAssetRegistryQuery, AssetRegistryDto?>
{
    public async ValueTask<AssetRegistryDto?> Handle(GetAssetRegistryQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var asset = await db.AssetRegistries
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.Id, ct).ConfigureAwait(false);
        return asset is null ? null : AssetRegistryMapper.ToDto(asset);
    }
}

