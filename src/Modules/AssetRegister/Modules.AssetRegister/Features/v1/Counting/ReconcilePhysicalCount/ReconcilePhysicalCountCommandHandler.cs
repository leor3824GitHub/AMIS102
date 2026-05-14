using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ReconcilePhysicalCount;

public sealed class ReconcilePhysicalCountCommandHandler(
    AssetRegisterDbContext db,
    ILogger<ReconcilePhysicalCountCommandHandler> logger)
    : ICommandHandler<ReconcilePhysicalCountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(ReconcilePhysicalCountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        // FoundAtStation entries require an operator-assigned PropertyNo per NFA policy.
        // The current ReconcilePhysicalCountCommand does not carry per-entry PropertyNos, so
        // we surface this as a warning and leave materialization to a separate RegisterAsset
        // flow until the physical-count UI is extended to capture PropertyNos.
        var unattached = session.Entries
            .Count(e => e.Condition == PhysicalCountCondition.FoundAtStation && e.AssetRegistryId is null);
        if (unattached > 0)
        {
            logger.LogWarning(
                "[{Tenant}] Session {SessionId}: {Count} FoundAtStation entries have no AssetRegistryId. Register them via RegisterAssetCommand with operator-supplied PropertyNos, then re-reconcile.",
                tenantId, session.Id, unattached);
        }

        // Domain reconcile raises AssetReportedMissingFromCountEvent — handled separately.
        session.Reconcile();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}

