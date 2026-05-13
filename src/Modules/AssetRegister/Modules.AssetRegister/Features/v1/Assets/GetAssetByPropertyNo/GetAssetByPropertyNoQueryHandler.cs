using FSH.Modules.AssetRegister.Contracts.v1.Assets;
using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Assets.GetAssetByPropertyNo;

public sealed class GetAssetByPropertyNoQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAssetByPropertyNoQuery, AssetRegistryDto?>
{
    public async ValueTask<AssetRegistryDto?> Handle(GetAssetByPropertyNoQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalized = query.PropertyNo?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || !PropertyNumber.TryParse(normalized, out var pn))
            return null;

        // Equality on the value-converted property translates to a string comparison
        // against the underlying column (configured via HasConversion).
        var asset = await db.AssetRegistries
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PropertyNo == pn, ct).ConfigureAwait(false);
        return asset is null ? null : AssetRegistryMapper.ToDto(asset);
    }
}
