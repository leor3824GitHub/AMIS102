using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.GetModesOfProcurement;

public sealed class GetModesOfProcurementQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetModesOfProcurementQuery, PagedResponseOfModeOfProcurementDto>
{
    public async ValueTask<PagedResponseOfModeOfProcurementDto> Handle(GetModesOfProcurementQuery query, CancellationToken cancellationToken)
    {
        var entityQuery = dbContext.ModesOfProcurement.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            entityQuery = entityQuery.Where(x =>
                x.Name.ToLower().Contains(keyword) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword)));
        }

        var totalCount = await entityQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var skipCount = (pageNumber - 1) * pageSize;

        var items = await entityQuery
            .OrderBy(x => x.Name)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(x => new ModeOfProcurementDto(
                x.Id,
                x.Name,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponseOfModeOfProcurementDto(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }
}
