using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.SearchPPEIRFormSeries;

public sealed class SearchPPEIRFormSeriesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchPPEIRFormSeriesQuery, PagedResponse<PPEIRFormSeriesDto>>
{
    public async ValueTask<PagedResponse<PPEIRFormSeriesDto>> Handle(SearchPPEIRFormSeriesQuery query, CancellationToken cancellationToken)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var q = db.PPEIRFormSeries
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.StartSerial);

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PPEIRFormSeriesDto>
        {
            Items = items.Select(PPEIRFormSeriesMapper.ToDto).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
        };
    }
}
