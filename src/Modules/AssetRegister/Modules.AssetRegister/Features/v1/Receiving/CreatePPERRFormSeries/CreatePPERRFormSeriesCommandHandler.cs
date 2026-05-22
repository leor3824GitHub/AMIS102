using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreatePPERRFormSeries;

public sealed class CreatePPERRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CreatePPERRFormSeriesCommand, PPERRFormSeriesDto>
{
    public async ValueTask<PPERRFormSeriesDto> Handle(CreatePPERRFormSeriesCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var overlapping = await db.PPERRFormSeries
            .Where(s => s.TenantId == tenantId &&
                        s.StartSerial <= cmd.EndSerial &&
                        s.EndSerial >= cmd.StartSerial)
            .AnyAsync(ct)
            .ConfigureAwait(false);

        if (overlapping)
            throw new InvalidOperationException(
                $"Serial range {cmd.StartSerial}–{cmd.EndSerial} overlaps with an existing series.");

        var series = PPERRFormSeries.Create(tenantId, cmd.Label, cmd.StartSerial, cmd.EndSerial);
        db.PPERRFormSeries.Add(series);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return PPERRFormSeriesMapper.ToDto(series);
    }
}
