using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;

public sealed class CreatePhysicalCountSessionCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<CreatePhysicalCountSessionCommand, CreatePhysicalCountSessionResult>
{
    public async ValueTask<CreatePhysicalCountSessionResult> Handle(
        CreatePhysicalCountSessionCommand command,
        CancellationToken cancellationToken)
    {
        var sessionNoInUse = await dbContext.PhysicalCountSessions
            .IgnoreQueryFilters()
            .AnyAsync(x => x.SessionNo == command.SessionNo, cancellationToken)
            .ConfigureAwait(false);

        if (sessionNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.SessionNo),
                    "A physical count session with this session number already exists.")
            ]);
        }

        var session = PhysicalCountSession.Create(
            command.SessionNo,
            command.CountDate,
            command.StationOffice,
            command.Scope,
            command.PreparedByEmployeeId,
            command.CertifiedByEmployeeId,
            command.ApprovedByEmployeeId);

        session.CreatedBy = currentUser.GetUserId().ToString();
        dbContext.PhysicalCountSessions.Add(session);

        int entriesCreated = 0;

        // Generate PPE checklist entries
        if (command.Scope is PhysicalCountScope.PPEOnly or PhysicalCountScope.Both)
        {
            var ppeItems = await dbContext.PPEItems
                .Where(x => x.Status != PPEItemStatus.Disposed)
                .Select(x => new { x.Id, x.PropertyNumber, x.Description, x.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var item in ppeItems)
            {
                var entry = PhysicalCountEntry.FromPPEItem(
                    session.Id,
                    item.Id,
                    item.PropertyNumber,
                    item.Description,
                    item.UnitCost);

                dbContext.PhysicalCountEntries.Add(entry);
                entriesCreated++;
            }
        }

        // Generate Semi-Expendable checklist entries
        if (command.Scope is PhysicalCountScope.SemiExpendableOnly or PhysicalCountScope.Both)
        {
            var seProperties = await dbContext.SemiExpendableProperties
                .Where(x => x.Status != PropertyStatus.Disposed)
                .Join(dbContext.SemiExpendableItems,
                    p => p.SemiExpendableItemId,
                    i => i.Id,
                    (p, i) => new { p.Id, p.PropertyNo, Description = i.Name, p.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var prop in seProperties)
            {
                var entry = PhysicalCountEntry.FromSemiExpendable(
                    session.Id,
                    prop.Id,
                    prop.PropertyNo,
                    prop.Description,
                    prop.UnitCost);

                dbContext.PhysicalCountEntries.Add(entry);
                entriesCreated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePhysicalCountSessionResult(session.Id, session.SessionNo, entriesCreated);
    }
}
