using FSH.Modules.AssetRegister.Contracts.v1.Catalog;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Features.v1.Catalog.CreatePropertyItemCatalog;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.SetPropertyItemCatalogActivation;

public sealed class SetPropertyItemCatalogActivationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<SetPropertyItemCatalogActivationCommand, PropertyItemCatalogDto>
{
    public async ValueTask<PropertyItemCatalogDto> Handle(SetPropertyItemCatalogActivationCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var entity = await db.PropertyItemCatalogs.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.Id}' not found.");

        if (cmd.IsActive) entity.Reactivate();
        else entity.Deactivate();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return PropertyItemCatalogMapper.ToDto(entity);
    }
}
