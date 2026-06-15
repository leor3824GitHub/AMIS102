using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.ActivatePPERRFormSeries;

public sealed class DeactivatePPERRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeactivatePPERRFormSeriesCommand, PPERRFormSeriesDto>
{
    public async ValueTask<PPERRFormSeriesDto> Handle(DeactivatePPERRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var series = await db.PPERRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPERR Form Series '{cmd.Id}' not found.");

        series.Deactivate();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PPERRFormSeriesMapper.ToDto(series);
    }
}
