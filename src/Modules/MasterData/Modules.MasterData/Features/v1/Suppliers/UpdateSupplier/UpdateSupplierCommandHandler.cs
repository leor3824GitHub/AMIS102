using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.UpdateSupplier;

public sealed class UpdateSupplierCommandHandler : ICommandHandler<UpdateSupplierCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateSupplierCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await _dbContext.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new KeyNotFoundException($"Supplier with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A supplier with this code already exists.")
            ]);
        }

        supplier.Update(
            command.Code,
            command.Name,
            command.TinNo,
            command.BusinessTaxType,
            command.Description,
            command.ContactPerson,
            command.Email,
            command.Phone,
            command.Address,
            command.IsActive);

        supplier.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.Suppliers.Update(supplier);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

