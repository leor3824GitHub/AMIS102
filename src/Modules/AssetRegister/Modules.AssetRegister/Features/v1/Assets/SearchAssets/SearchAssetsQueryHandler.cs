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
    public async ValueTask<PagedResponse<AssetRegistrySummaryDto>> Handle(SearchAssetsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.AssetRegistries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            // ILIKE (case-insensitive) instead of lower(col).Contains — removes the per-row lower() call
            // and is the predicate a pg_trgm GIN index accelerates. Matches the SearchProducts convention.
            var pattern = $"%{query.Keyword}%";
            q = q.Where(a =>
                EF.Functions.ILike(a.Description, pattern) ||
                (a.SerialNo != null && EF.Functions.ILike(a.SerialNo, pattern)) ||
                (a.Brand != null && EF.Functions.ILike(a.Brand, pattern)) ||
                (a.Model != null && EF.Functions.ILike(a.Model, pattern)));
        }

        // Serial-only filter — precise lookup used by the mobile "search by serial number" mode.
        if (!string.IsNullOrWhiteSpace(query.SerialNo))
        {
            var serialPattern = $"%{query.SerialNo.Trim()}%";
            q = q.Where(a => a.SerialNo != null && EF.Functions.ILike(a.SerialNo, serialPattern));
        }
        if (!string.IsNullOrWhiteSpace(query.PropertyClass))
        {
            var pc = query.PropertyClass.Trim();
            q = q.Where(a => a.PropertyClass == pc);
        }
        if (query.AssetType.HasValue) q = q.Where(a => a.AssetType == query.AssetType.Value);
        if (query.LifecycleState.HasValue) q = q.Where(a => a.LifecycleState == query.LifecycleState.Value);
        else if (!query.IncludeTransferredOut) q = q.Where(a => a.LifecycleState != LifecycleState.TransferredOut);
        if (query.CurrentCustodianId.HasValue) q = q.Where(a => a.CurrentCustodianId == query.CurrentCustodianId.Value);
        if (query.CurrentCondition.HasValue) q = q.Where(a => a.CurrentCondition == query.CurrentCondition.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        // Project only the summary's columns in the DB instead of materializing the full ~25-column entity.
        // PropertyNo (a value-converted VO) is projected as the VO and mapped via .Value in memory — the same
        // proven pattern used by GetReconciliationReportQueryHandler.
        var page = await q
            .OrderByDescending(a => a.AcquisitionDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.PropertyNo,
                a.AssetType,
                a.Category,
                a.Description,
                a.UnitCost,
                a.FundCluster,
                a.AcquisitionDate,
                a.LifecycleState,
                a.CurrentCondition,
                a.CurrentCustodianId,
                // Project only the presence of a photo (IS NOT NULL) — never the multi-MB base64 blob.
                // Clients fetch the picture lazily per row from GET /assets/{id}/image.
                HasImage = a.ImageUrl != null
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = page.ConvertAll(a => new AssetRegistrySummaryDto(
            a.Id, a.PropertyNo.Value, a.AssetType, a.Category, a.Description, a.UnitCost, a.FundCluster,
            a.AcquisitionDate, a.LifecycleState, a.CurrentCondition, a.CurrentCustodianId, a.HasImage));

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

