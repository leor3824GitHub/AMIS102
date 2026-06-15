using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.UpdatePropertyItemCatalog;

public sealed class UpdatePropertyItemCatalogCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdatePropertyItemCatalogCommand, PropertyItemCatalogDto>
{
    public async ValueTask<PropertyItemCatalogDto> Handle(UpdatePropertyItemCatalogCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var entity = await db.PropertyItemCatalogs.FirstOrDefaultAsync(x => x.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.Id}' not found.");

        entity.Update(
            cmd.Description,
            cmd.DefaultPropertyClass,
            cmd.DefaultCategoryCode,
            cmd.DefaultUnit,
            cmd.UacsObjectCode,
            cmd.EstimatedUsefulLifeYears,
            cmd.ResidualValuePercent,
            cmd.DepreciationMethod);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyItemCatalogMapper.ToDto(entity);
    }
}

