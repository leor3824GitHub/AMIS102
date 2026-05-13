using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.RecordPhysicalCountEntry;

public sealed class RecordPhysicalCountEntryCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordPhysicalCountEntryCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(RecordPhysicalCountEntryCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        session.RecordEntry(asset, cmd.Article, cmd.Unit, cmd.UnitCost, cmd.Condition,
            cmd.LocationId, cmd.ScannedOnUtc, cmd.ScannedByEmployeeId, cmd.PhotoPath, cmd.Remarks);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}

