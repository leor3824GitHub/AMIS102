using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetAccountability;

public sealed class GetAccountabilityQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAccountabilityQuery, PropertyAccountabilityDto?>
{
    public async ValueTask<PropertyAccountabilityDto?> Handle(GetAccountabilityQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var entity = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;

        // Presence-flag-only lookup (mirrors the physical-count checklist): tells the mobile client
        // which lines' assets have a photo, without ever loading the image bytes into this payload.
        var assetIds = entity.Lines.Select(l => l.AssetRegistryId).Distinct().ToList();
        var withImage = (await db.AssetRegistries.AsNoTracking()
            .Where(a => assetIds.Contains(a.Id) && a.ImageUrl != null)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();

        return AccountabilityMapper.ToDto(entity, withImage);
    }
}

