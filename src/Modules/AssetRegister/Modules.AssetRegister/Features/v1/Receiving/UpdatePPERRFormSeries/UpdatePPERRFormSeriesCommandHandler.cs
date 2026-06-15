using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.UpdatePPERRFormSeries;

public sealed class UpdatePPERRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdatePPERRFormSeriesCommand, PPERRFormSeriesDto>
{
    public async ValueTask<PPERRFormSeriesDto> Handle(UpdatePPERRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var series = await db.PPERRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPERR Form Series '{cmd.Id}' not found.");

        var overlapping = await db.PPERRFormSeries
            .Where(s => s.TenantId == tenantId &&
                        s.Id != cmd.Id &&
                        s.StartSerial <= cmd.EndSerial &&
                        s.EndSerial >= cmd.StartSerial)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (overlapping)
            throw new InvalidOperationException(
                $"Serial range {cmd.StartSerial}–{cmd.EndSerial} overlaps with an existing series.");

        series.UpdateRange(cmd.Label, cmd.StartSerial, cmd.EndSerial);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return PPERRFormSeriesMapper.ToDto(series);
    }
}
