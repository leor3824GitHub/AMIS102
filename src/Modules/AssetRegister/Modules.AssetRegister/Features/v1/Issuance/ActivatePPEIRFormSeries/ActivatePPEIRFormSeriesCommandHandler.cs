using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.ActivatePPEIRFormSeries;

public sealed class ActivatePPEIRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<ActivatePPEIRFormSeriesCommand, PPEIRFormSeriesDto>
{
    public async ValueTask<PPEIRFormSeriesDto> Handle(ActivatePPEIRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var target = await db.PPEIRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPEIR Form Series '{cmd.Id}' not found.");

        var currentActive = await db.PPEIRFormSeries
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.Id != cmd.Id, cancellationToken)
            .ConfigureAwait(false);

        currentActive?.Deactivate();
        target.Activate();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PPEIRFormSeriesMapper.ToDto(target);
    }
}
