using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.SearchAssets;

public sealed class SearchAssetsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchAssetsQuery, PagedResponse<AssetRegistrySummaryDto>>
{
    public async ValueTask<PagedResponse<AssetRegistrySummaryDto>> Handle(SearchAssetsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.AssetRegistries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(a =>
                a.Description.ToLower().Contains(k) ||
                (a.SerialNo != null && a.SerialNo.ToLower().Contains(k)) ||
                (a.Brand != null && a.Brand.ToLower().Contains(k)) ||
                (a.Model != null && a.Model.ToLower().Contains(k)));
        }
        if (query.AssetType.HasValue) q = q.Where(a => a.AssetType == query.AssetType.Value);
        if (query.LifecycleState.HasValue) q = q.Where(a => a.LifecycleState == query.LifecycleState.Value);
        else if (!query.IncludeTransferredOut) q = q.Where(a => a.LifecycleState != LifecycleState.TransferredOut);
        if (query.CurrentCustodianId.HasValue) q = q.Where(a => a.CurrentCustodianId == query.CurrentCustodianId.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var page = await q
            .OrderByDescending(a => a.AcquisitionDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        var items = page.ConvertAll(a => new AssetRegistrySummaryDto(
            a.Id, a.PropertyNo.Value, a.AssetType, a.Description, a.UnitCost,
            a.AcquisitionDate, a.LifecycleState, a.CurrentCondition, a.CurrentCustodianId));

        return new PagedResponse<AssetRegistrySummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

