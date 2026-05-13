using FSH.Modules.AssetRegister.Contracts.v1.Catalog;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.UpdatePropertyItemCatalog;

public sealed class UpdatePropertyItemCatalogCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdatePropertyItemCatalogCommand, PropertyItemCatalogDto>
{
    public async ValueTask<PropertyItemCatalogDto> Handle(UpdatePropertyItemCatalogCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var entity = await db.PropertyItemCatalogs.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.Id}' not found.");

        entity.Update(
            cmd.Description,
            cmd.DefaultPropertyClass,
            cmd.DefaultCategoryCode,
            cmd.DefaultUnit,
            cmd.UacsObjectCode,
            cmd.EstimatedUsefulLifeYears);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return PropertyItemCatalogMapper.ToDto(entity);
    }
}
