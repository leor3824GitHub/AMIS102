using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.ActivatePPERRFormSeries;

public sealed class ActivatePPERRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<ActivatePPERRFormSeriesCommand, PPERRFormSeriesDto>
{
    public async ValueTask<PPERRFormSeriesDto> Handle(ActivatePPERRFormSeriesCommand cmd, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var target = await db.PPERRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPERR Form Series '{cmd.Id}' not found.");

        var currentActive = await db.PPERRFormSeries
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.Id != cmd.Id, ct)
            .ConfigureAwait(false);

        currentActive?.Deactivate();
        target.Activate();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return PPERRFormSeriesMapper.ToDto(target);
    }
}
