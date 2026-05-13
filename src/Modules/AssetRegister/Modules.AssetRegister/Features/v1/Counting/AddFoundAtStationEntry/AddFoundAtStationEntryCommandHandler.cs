using FSH.Modules.AssetRegister.Contracts.v1.Counting;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Counting.AddFoundAtStationEntry;

public sealed class AddFoundAtStationEntryCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AddFoundAtStationEntryCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(AddFoundAtStationEntryCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var session = await db.PhysicalCountSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{cmd.SessionId}' not found.");

        session.AddFoundAtStationEntry(cmd.Article, cmd.Unit, cmd.UnitCost, cmd.LocationId,
            cmd.ProposedPropertyClass, cmd.ProposedCategoryCode, cmd.ProposedAcquisitionDate,
            cmd.ProposedUnitCost, cmd.ScannedByEmployeeId, cmd.Remarks);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}
