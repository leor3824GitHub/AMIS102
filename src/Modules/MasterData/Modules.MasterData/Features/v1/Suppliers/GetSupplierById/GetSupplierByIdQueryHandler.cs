using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Suppliers.GetSupplierById;

public sealed class GetSupplierByIdQueryHandler : IQueryHandler<GetSupplierByIdQuery, SupplierDetailsDto>
{
    private readonly MasterDataDbContext _dbContext;

    public GetSupplierByIdQueryHandler(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<SupplierDetailsDto> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        var supplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new KeyNotFoundException($"Supplier with ID {query.Id} not found.");
        }

        return new SupplierDetailsDto(
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
            supplier.OfficeCode,
            supplier.CreatedOnUtc,
            supplier.CreatedBy,
            supplier.LastModifiedOnUtc,
            supplier.LastModifiedBy);
    }
}
