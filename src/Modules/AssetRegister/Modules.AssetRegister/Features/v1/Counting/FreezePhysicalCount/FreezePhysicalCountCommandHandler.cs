using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.FreezePhysicalCount;

public sealed class FreezePhysicalCountCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<FreezePhysicalCountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(FreezePhysicalCountCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        session.Freeze(cmd.OfficeOrderNo, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}
