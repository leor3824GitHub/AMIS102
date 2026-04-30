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

        string tenantId = currentUser.GetTenant() ?? string.Empty;

        var session = PhysicalCountSession.Create(
            tenantId,
            command.SessionNo,
            command.CountDate,
            command.StationOffice,
            command.Scope,
            command.PreparedByEmployeeId,
            command.CertifiedByEmployeeId,
            command.ApprovedByEmployeeId);

        session.CreatedBy = currentUser.GetUserId().ToString();
        dbContext.PhysicalCountSessions.Add(session);

        // Build the asset filter based on scope.
        // Scope maps to AssetType on TangibleInventoryItem (SE or PPE).
        var query = dbContext.TangibleInventoryItems.AsQueryable();

        query = command.Scope switch
        {
            PhysicalCountScope.PPEOnly => query.Where(x => x.AssetType == AssetType.PPE),
            PhysicalCountScope.SemiExpendableOnly => query.Where(x => x.AssetType == AssetType.SE),
            _ => query, // Both — no filter
        };

        var invItems = await query
            .Select(x => new { x.Id, x.PropertyNo, x.Description, x.UnitCost })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int entriesCreated = 0;
        foreach (var item in invItems)
        {
            var entry = PhysicalCountEntry.FromTangibleInventoryItem(
                tenantId,
                session.Id,
                item.Id,
                item.PropertyNo,
                item.Description ?? string.Empty,
                item.UnitCost);

            dbContext.PhysicalCountEntries.Add(entry);
            entriesCreated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePhysicalCountSessionResult(session.Id, session.SessionNo, entriesCreated);
    }
}
