using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ClosePhysicalCount;

public sealed class ClosePhysicalCountCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<ClosePhysicalCountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(ClosePhysicalCountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var approvedBy = EmployeeRef.Create(cmd.ApprovedBy.EmployeeId, cmd.ApprovedBy.PrintedName, cmd.ApprovedBy.Designation);
        EmployeeRef? witnessedBy = cmd.WitnessedBy is null ? null
            : EmployeeRef.Create(cmd.WitnessedBy.EmployeeId, cmd.WitnessedBy.PrintedName, cmd.WitnessedBy.Designation);

        session.Close(approvedBy, witnessedBy, cmd.ClosedOn);

        // Mirror per-entry condition onto the AssetRegistry now that the session is Closed.
        var assetEntries = session.Entries
            .Where(e => e.AssetRegistryId is not null && e.Condition != PhysicalCountCondition.Missing && e.Condition != PhysicalCountCondition.FoundAtStation)
            .ToList();
        if (assetEntries.Count > 0)
        {
            var assetIds = assetEntries.Select(e => e.AssetRegistryId!.Value).ToList();
            var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
            var byId = assets.ToDictionary(a => a.Id);
            foreach (var entry in assetEntries)
            {
                if (!byId.TryGetValue(entry.AssetRegistryId!.Value, out var asset)) continue;
                var mapped = entry.Condition switch
                {
                    PhysicalCountCondition.InGoodCondition => AssetCondition.InGoodCondition,
                    PhysicalCountCondition.NeedingRepair => AssetCondition.NeedingRepair,
                    PhysicalCountCondition.Unserviceable => AssetCondition.Unserviceable,
                    _ => (AssetCondition?)null
                };
                if (mapped.HasValue) asset.UpdateCondition(mapped.Value);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}

