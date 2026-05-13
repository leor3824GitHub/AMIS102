using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.RecordPhysicalCountEntry;

public sealed class RecordPhysicalCountEntryCommandHandler(AssetManagementDbContext dbContext)
    : ICommandHandler<RecordPhysicalCountEntryCommand, RecordPhysicalCountEntryResult>
{
    public async ValueTask<RecordPhysicalCountEntryResult> Handle(
        RecordPhysicalCountEntryCommand command,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {command.SessionId} not found.");

        if (session.Status == PhysicalCountStatus.Submitted)
            throw new CustomException("Cannot record entries on a submitted physical count session.", Array.Empty<string>(), HttpStatusCode.Conflict);

        var entry = await dbContext.PhysicalCountEntries
            .FirstOrDefaultAsync(x => x.Id == command.EntryId && x.SessionId == command.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
            throw new NotFoundException($"Physical count entry {command.EntryId} not found in session {command.SessionId}.");

        switch (command.Result)
        {
            case PhysicalCountEntryResult.Found when command.IsScanned:
                entry.MarkFoundViaScan(command.Condition!.Value, command.QuantityOnHand, command.Remarks, command.PhotoPath);
                break;

            case PhysicalCountEntryResult.Found:
                entry.MarkFound(command.Condition!.Value, command.QuantityOnHand, command.Remarks);
                break;

            case PhysicalCountEntryResult.NotFound:
                entry.MarkNotFound(command.Remarks);
                break;

            case PhysicalCountEntryResult.FoundAtStation:
                // FoundAtStation is handled via the AddFoundAtStationEntry command.
                // Updating an existing checklist entry to FoundAtStation is treated as Found.
                entry.MarkFound(command.Condition!.Value, command.QuantityOnHand, command.Remarks);
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RecordPhysicalCountEntryResult(
            entry.Id,
            entry.PropertyNumber,
            entry.Result!.Value.ToString(),
            entry.Condition?.ToString(),
            entry.ScannedOnUtc.HasValue);
    }
}

