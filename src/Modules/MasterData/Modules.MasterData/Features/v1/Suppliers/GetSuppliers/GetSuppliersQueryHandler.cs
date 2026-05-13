using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.GetSuppliers;

public sealed class GetSuppliersQueryHandler(MasterDataDbContext dbContext) : IQueryHandler<GetSuppliersQuery, PagedResponseOfSupplierDto>
{
    public async ValueTask<PagedResponseOfSupplierDto> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        var suppliersQuery = dbContext.Suppliers.AsQueryable();

        // Apply keyword filter if provided
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            var isTaxKeyword = keyword is "vat" or "non-vat";
            suppliersQuery = suppliersQuery.Where(s =>
                s.Code.ToLower().Contains(keyword) ||
                s.Name.ToLower().Contains(keyword) ||
                (s.TinNo != null && s.TinNo.ToLower().Contains(keyword)) ||
                (isTaxKeyword && s.BusinessTaxType.ToLower() == keyword) ||
                (s.Description != null && s.Description.ToLower().Contains(keyword)) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(keyword)) ||
                (s.Email != null && s.Email.ToLower().Contains(keyword)));
        }

        // Get total count before pagination
        var totalCount = await suppliersQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply pagination
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var skipCount = (pageNumber - 1) * pageSize;

        var suppliers = await suppliersQuery
            .OrderBy(s => s.Code)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.TinNo,
                s.BusinessTaxType,
                s.Description,
                s.ContactPerson,
                s.Email,
                s.Phone,
                s.Address,
                s.IsActive,
                s.OfficeCode))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponseOfSupplierDto(
            suppliers,
            pageNumber,
            pageSize,
            totalCount);
    }
}

