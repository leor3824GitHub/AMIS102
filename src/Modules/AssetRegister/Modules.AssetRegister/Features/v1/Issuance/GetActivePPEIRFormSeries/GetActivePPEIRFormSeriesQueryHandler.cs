using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.GetActivePPEIRFormSeries;

public sealed class GetActivePPEIRFormSeriesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetActivePPEIRFormSeriesQuery, PPEIRFormSeriesDto?>
{
    public async ValueTask<PPEIRFormSeriesDto?> Handle(GetActivePPEIRFormSeriesQuery query, CancellationToken cancellationToken)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var series = await db.PPEIRFormSeries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive, cancellationToken)
            .ConfigureAwait(false);
        return series is null ? null : PPEIRFormSeriesMapper.ToDto(series);
    }
}
