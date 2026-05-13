using FSH.Modules.AssetRegister.Contracts.v1.Catalog;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Catalog.DeletePropertyItemCatalog;

public sealed class DeletePropertyItemCatalogCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeletePropertyItemCatalogCommand>
{
    public async ValueTask<Unit> Handle(DeletePropertyItemCatalogCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var entity = await db.PropertyItemCatalogs.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.Id}' not found.");

        var inUse = await db.AssetRegistries.AnyAsync(a => a.ItemId == cmd.Id, ct).ConfigureAwait(false);
        if (inUse)
            throw new InvalidOperationException("Catalog item is referenced by registered assets and cannot be deleted. Deactivate it instead.");

        db.PropertyItemCatalogs.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
