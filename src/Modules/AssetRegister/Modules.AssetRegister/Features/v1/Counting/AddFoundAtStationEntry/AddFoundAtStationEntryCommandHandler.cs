using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.AddFoundAtStationEntry;

public sealed class AddFoundAtStationEntryCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AddFoundAtStationEntryCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(AddFoundAtStationEntryCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        // See RecordPhysicalCountEntryCommandHandler: appends do not touch the session row, so
        // concurrent counters issue independent INSERTs without contending on optimistic concurrency.
        session.AddFoundAtStationEntry(cmd.Article, cmd.Unit, cmd.UnitCost, cmd.LocationId,
            cmd.ProposedPropertyClass, cmd.ProposedCategoryCode, cmd.ProposedAcquisitionDate,
            cmd.ProposedUnitCost, cmd.ProposedPropertyNo, cmd.ProposedCatalogItemId,
            cmd.ScannedByEmployeeId, cmd.Remarks);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}
