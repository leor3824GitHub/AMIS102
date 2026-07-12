using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.GetPropertyItemCatalogsByIds;

public sealed class GetPropertyItemCatalogsByIdsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPropertyItemCatalogsByIdsQuery, IReadOnlyList<PropertyItemCatalogDto>>
{
    public async ValueTask<IReadOnlyList<PropertyItemCatalogDto>> Handle(
        GetPropertyItemCatalogsByIdsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Distinct, non-empty ids only — one WHERE Id IN (...) round-trip, never a per-id N+1.
        var ids = query.Ids.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        return await db.PropertyItemCatalogs
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new PropertyItemCatalogDto(
                x.Id, x.Code, x.Description, x.DefaultPropertyClass, x.DefaultCategoryCode,
                x.DefaultUnit, x.UacsObjectCode, x.EstimatedUsefulLifeYears, x.IsActive, x.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
