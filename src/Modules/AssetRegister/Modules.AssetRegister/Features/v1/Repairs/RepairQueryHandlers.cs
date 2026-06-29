using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class GetPropertyRepairQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPropertyRepairQuery, PropertyRepairDto?>
{
    public async ValueTask<PropertyRepairDto?> Handle(GetPropertyRepairQuery query, CancellationToken cancellationToken)
    {
        var row = await (from r in db.PropertyRepairs.AsNoTracking()
                         join a in db.AssetRegistries.AsNoTracking() on r.AssetRegistryId equals a.Id
                         where r.Id == query.Id
                         select new { r, a }).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return row is null ? null : row.r.ToDto(row.a);
    }
}

public sealed class GetLastRepairQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetLastRepairQuery, PropertyRepairDto?>
{
    public async ValueTask<PropertyRepairDto?> Handle(GetLastRepairQuery query, CancellationToken cancellationToken)
    {
        var row = await (from r in db.PropertyRepairs.AsNoTracking()
                         join a in db.AssetRegistries.AsNoTracking() on r.AssetRegistryId equals a.Id
                         where r.AssetRegistryId == query.AssetRegistryId && r.Status == RepairStatus.Accepted
                         orderby r.AcceptedOn descending
                         select new { r, a }).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return row is null ? null : row.r.ToDto(row.a);
    }
}

public sealed class GetAssetRepairHistoryQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAssetRepairHistoryQuery, List<PropertyRepairSummaryDto>>
{
    public async ValueTask<List<PropertyRepairSummaryDto>> Handle(GetAssetRepairHistoryQuery query, CancellationToken cancellationToken)
    {
        var rows = await (from r in db.PropertyRepairs.AsNoTracking()
                          join a in db.AssetRegistries.AsNoTracking() on r.AssetRegistryId equals a.Id
                          where r.AssetRegistryId == query.AssetRegistryId && r.Status == RepairStatus.Accepted
                          orderby r.AcceptedOn descending
                          select new { r, a }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ConvertAll(x => x.r.ToSummary(x.a));
    }
}

public sealed class SearchPropertyRepairsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchPropertyRepairsQuery, PagedResponse<PropertyRepairSummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyRepairSummaryDto>> Handle(SearchPropertyRepairsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = from r in db.PropertyRepairs.AsNoTracking()
                join a in db.AssetRegistries.AsNoTracking() on r.AssetRegistryId equals a.Id
                select new { r, a };

        if (query.AssetRegistryId.HasValue)
            q = q.Where(x => x.r.AssetRegistryId == query.AssetRegistryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RepairStatus>(query.Status, true, out var status))
            q = q.Where(x => x.r.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(x => x.r.RpriNo.ToLower().Contains(k)
                || x.r.NatureOfWork.ToLower().Contains(k)
                || x.a.Description.ToLower().Contains(k));
        }

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var page = await q
            .OrderByDescending(x => x.r.RequestedOn)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<PropertyRepairSummaryDto>
        {
            Items = page.ConvertAll(x => x.r.ToSummary(x.a)),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
