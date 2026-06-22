using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.DeletePPEIRFormSeries;

public sealed class DeletePPEIRFormSeriesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeletePPEIRFormSeriesCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePPEIRFormSeriesCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var series = await db.PPEIRFormSeries
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPEIR Form Series '{cmd.Id}' not found.");

        if (!series.IsUnused)
            throw new InvalidOperationException(
                $"Series '{series.Label}' has already issued PPEIR numbers and cannot be deleted.");

        db.PPEIRFormSeries.Remove(series);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
