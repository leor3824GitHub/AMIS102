using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.ActivatePPEIRFormSeries;

public sealed class DeactivatePPEIRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeactivatePPEIRFormSeriesCommand, PPEIRFormSeriesDto>
{
    public async ValueTask<PPEIRFormSeriesDto> Handle(DeactivatePPEIRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var series = await db.PPEIRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPEIR Form Series '{cmd.Id}' not found.");

        series.Deactivate();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PPEIRFormSeriesMapper.ToDto(series);
    }
}
