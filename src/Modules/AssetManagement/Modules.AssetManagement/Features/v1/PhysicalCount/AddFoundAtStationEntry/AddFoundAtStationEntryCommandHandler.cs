using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.AddFoundAtStationEntry;

public sealed class AddFoundAtStationEntryCommandHandler(AssetManagementDbContext dbContext)
    : ICommandHandler<AddFoundAtStationEntryCommand, AddFoundAtStationEntryResult>
{
    public async ValueTask<AddFoundAtStationEntryResult> Handle(
        AddFoundAtStationEntryCommand command,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new KeyNotFoundException($"Physical count session {command.SessionId} not found.");

        if (session.Status == PhysicalCountStatus.Submitted)
            throw new InvalidOperationException("Cannot add entries to a submitted physical count session.");

        var entry = PhysicalCountEntry.CreateFoundAtStation(
            session.TenantId,
            command.SessionId,
            command.PropertyNumber,
            command.Description,
            command.UnitCost,
            command.Condition,
            command.Remarks,
            command.PhotoPath);

        dbContext.PhysicalCountEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AddFoundAtStationEntryResult(entry.Id, entry.PropertyNumber);
    }
}
