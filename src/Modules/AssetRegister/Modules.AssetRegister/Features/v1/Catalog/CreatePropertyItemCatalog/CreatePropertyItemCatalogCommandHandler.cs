using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;

public sealed class CreatePropertyItemCatalogCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CreatePropertyItemCatalogCommand, PropertyItemCatalogDto>
{
    public async ValueTask<PropertyItemCatalogDto> Handle(CreatePropertyItemCatalogCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var codeInUse = await db.PropertyItemCatalogs
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == cmd.Code, ct).ConfigureAwait(false);
        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(cmd.Code), "A catalog item with this code already exists.")
            ]);
        }

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var entity = PropertyItemCatalog.Create(
            tenantId,
            cmd.Code,
            cmd.Description,
            cmd.DefaultPropertyClass,
            cmd.DefaultCategoryCode,
            cmd.DefaultUnit,
            cmd.UacsObjectCode,
            cmd.EstimatedUsefulLifeYears);

        db.PropertyItemCatalogs.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return PropertyItemCatalogMapper.ToDto(entity);
    }
}

internal static class PropertyItemCatalogMapper
{
    public static PropertyItemCatalogDto ToDto(PropertyItemCatalog x) =>
        new(x.Id, x.Code, x.Description, x.DefaultPropertyClass, x.DefaultCategoryCode,
            x.DefaultUnit, x.UacsObjectCode, x.EstimatedUsefulLifeYears, x.IsActive);
}

