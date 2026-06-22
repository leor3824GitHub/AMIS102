using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.DeletePPERRFormSeries;

public sealed class DeletePPERRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeletePPERRFormSeriesCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePPERRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var series = await db.PPERRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPERR Form Series '{cmd.Id}' not found.");

        if (series.IsActive)
            throw new InvalidOperationException(
                "Deactivate the series before deleting it.");

        if (!series.IsUnused)
            throw new InvalidOperationException(
                $"Series {series.StartSerial}–{series.EndSerial} has already issued PPERR numbers and cannot be deleted.");

        db.PPERRFormSeries.Remove(series);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
