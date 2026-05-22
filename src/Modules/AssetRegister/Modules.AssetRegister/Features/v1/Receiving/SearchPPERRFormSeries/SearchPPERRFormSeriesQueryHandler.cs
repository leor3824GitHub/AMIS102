using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.SearchPPERRFormSeries;

public sealed class SearchPPERRFormSeriesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchPPERRFormSeriesQuery, PagedResponse<PPERRFormSeriesDto>>
{
    public async ValueTask<PagedResponse<PPERRFormSeriesDto>> Handle(SearchPPERRFormSeriesQuery query, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var q = db.PPERRFormSeries
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.StartSerial);

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var items = await q
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResponse<PPERRFormSeriesDto>
        {
            Items = items.Select(PPERRFormSeriesMapper.ToDto).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
        };
    }
}
