using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.SubmitPhysicalCountSession;

public sealed class SubmitPhysicalCountSessionCommandHandler(AssetManagementDbContext dbContext)
    : ICommandHandler<SubmitPhysicalCountSessionCommand, SubmitPhysicalCountSessionResult>
{
    public async ValueTask<SubmitPhysicalCountSessionResult> Handle(
        SubmitPhysicalCountSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {command.SessionId} not found.");

        var entries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == command.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Auto-mark any unverified checklist entries as NotFound
        foreach (var entry in entries.Where(e => e.Result is null))
        {
            entry.MarkNotFound(remarks: "Not verified during physical count.");
        }

        session.Submit();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SubmitPhysicalCountSessionResult(
            session.Id,
            session.SessionNo,
            TotalEntries: entries.Count,
            Found:        entries.Count(e => e.Result == PhysicalCountEntryResult.Found),
            NotFound:     entries.Count(e => e.Result == PhysicalCountEntryResult.NotFound),
            FoundAtStation: entries.Count(e => e.Result == PhysicalCountEntryResult.FoundAtStation),
            SubmittedOnUtc: session.SubmittedOnUtc!.Value);
    }
}
