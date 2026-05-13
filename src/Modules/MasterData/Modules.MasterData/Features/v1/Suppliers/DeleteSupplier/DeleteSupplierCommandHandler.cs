using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.DeleteSupplier;

public sealed class DeleteSupplierCommandHandler : ICommandHandler<DeleteSupplierCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteSupplierCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new KeyNotFoundException($"Supplier with ID {command.Id} not found.");
        }

        supplier.DeletedOnUtc = DateTimeOffset.UtcNow;
        supplier.DeletedBy = _currentUser.GetUserId().ToString();
        supplier.IsDeleted = true;

        _dbContext.Suppliers.Update(supplier);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

