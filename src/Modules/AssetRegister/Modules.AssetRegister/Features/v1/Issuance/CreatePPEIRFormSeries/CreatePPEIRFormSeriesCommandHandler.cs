using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreatePPEIRFormSeries;

public sealed class CreatePPEIRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CreatePPEIRFormSeriesCommand, PPEIRFormSeriesDto>
{
    public async ValueTask<PPEIRFormSeriesDto> Handle(CreatePPEIRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var overlapping = await db.PPEIRFormSeries
            .Where(s => s.TenantId == tenantId &&
                        s.StartSerial <= cmd.EndSerial &&
                        s.EndSerial >= cmd.StartSerial)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (overlapping)
            throw new InvalidOperationException(
                $"Serial range {cmd.StartSerial}–{cmd.EndSerial} overlaps with an existing series.");

        var series = PPEIRFormSeries.Create(tenantId, cmd.StartSerial, cmd.EndSerial);
        db.PPEIRFormSeries.Add(series);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return PPEIRFormSeriesMapper.ToDto(series);
    }
}
