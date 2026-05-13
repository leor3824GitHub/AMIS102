using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.DeleteReceivingReport;

public sealed class DeleteReceivingReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeleteReceivingReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteReceivingReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var report = await db.ReceivingReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"ReceivingReport '{cmd.Id}' not found.");

        db.ReceivingReports.Remove(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

