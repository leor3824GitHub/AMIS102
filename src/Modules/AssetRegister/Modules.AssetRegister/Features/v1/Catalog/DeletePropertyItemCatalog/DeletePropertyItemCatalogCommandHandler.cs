using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.DeletePropertyItemCatalog;

public sealed class DeletePropertyItemCatalogCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeletePropertyItemCatalogCommand>
{
    public async ValueTask<Unit> Handle(DeletePropertyItemCatalogCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var entity = await db.PropertyItemCatalogs.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PropertyItemCatalog '{cmd.Id}' not found.");

        db.PropertyItemCatalogs.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

