using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.MarkPhysicalCountMissing;

public sealed class MarkPhysicalCountMissingCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<MarkPhysicalCountMissingCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(MarkPhysicalCountMissingCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        session.MarkMissing(asset, cmd.LocationId, cmd.Remarks);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}

