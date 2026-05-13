using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.GetPropertyItemCatalog;

public sealed class GetPropertyItemCatalogQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPropertyItemCatalogQuery, PropertyItemCatalogDto?>
{
    public async ValueTask<PropertyItemCatalogDto?> Handle(GetPropertyItemCatalogQuery query, CancellationToken ct) =>
        await db.PropertyItemCatalogs
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new PropertyItemCatalogDto(
                x.Id, x.Code, x.Description, x.DefaultPropertyClass, x.DefaultCategoryCode,
                x.DefaultUnit, x.UacsObjectCode, x.EstimatedUsefulLifeYears, x.IsActive))
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
}

