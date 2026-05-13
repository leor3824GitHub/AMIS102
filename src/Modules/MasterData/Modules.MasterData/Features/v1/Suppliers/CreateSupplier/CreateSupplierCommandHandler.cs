using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;

public sealed class CreateSupplierCommandHandler : ICommandHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateSupplierCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SupplierDto> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A supplier with this code already exists.")
            ]);
        }

        var supplier = Supplier.Create(
            command.Code,
            command.Name,
            command.TinNo,
            command.BusinessTaxType,
            command.Description,
            command.ContactPerson,
            command.Email,
            command.Phone,
            command.Address,
            command.OfficeCode);

        supplier.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SupplierDto(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.TinNo,
            supplier.BusinessTaxType,
            supplier.Description,
            supplier.ContactPerson,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.IsActive,
            supplier.OfficeCode);
    }
}

