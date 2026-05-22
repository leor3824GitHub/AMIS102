using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetActivePPERRFormSeries;

public sealed class GetActivePPERRFormSeriesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetActivePPERRFormSeriesQuery, PPERRFormSeriesDto?>
{
    public async ValueTask<PPERRFormSeriesDto?> Handle(GetActivePPERRFormSeriesQuery query, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var series = await db.PPERRFormSeries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive, ct)
            .ConfigureAwait(false);
        return series is null ? null : PPERRFormSeriesMapper.ToDto(series);
    }
}
