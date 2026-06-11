using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.RequestPhysicalCountRecount;

public sealed class RequestPhysicalCountRecountCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RequestPhysicalCountRecountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(RequestPhysicalCountRecountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        // Domain enforces Reconciled status + entry existence; flags the entry for re-recording.
        session.RequestRecount(cmd.EntryId, cmd.Reason);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}
