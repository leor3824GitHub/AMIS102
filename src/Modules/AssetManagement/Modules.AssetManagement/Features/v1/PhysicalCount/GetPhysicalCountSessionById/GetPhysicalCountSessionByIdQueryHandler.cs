using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionById;

public sealed class GetPhysicalCountSessionByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPhysicalCountSessionByIdQuery, PhysicalCountSessionDetailsDto>
{
    public async ValueTask<PhysicalCountSessionDetailsDto> Handle(
        GetPhysicalCountSessionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {query.Id} not found.");

        var entries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == query.Id)
            .OrderBy(x => x.PropertyNumber)
            .Select(x => new PhysicalCountEntryDto(
                x.Id,
                x.TangibleInventoryItemId,
                x.PropertyNumber,
                x.Description,
                x.UnitCost,
                x.Result != null ? x.Result.ToString() : null,
                x.Condition != null ? x.Condition.ToString() : null,
                x.QuantityOnHand,
                x.Remarks,
                x.ScannedOnUtc.HasValue,
                x.ScannedOnUtc,
                x.PhotoPath))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PhysicalCountSessionDetailsDto(
            session.Id,
            session.SessionNo,
            session.CountDate,
            session.StationOffice,
            session.Scope.ToString(),
            session.Status.ToString(),
            session.PreparedByEmployeeId,
            session.CertifiedByEmployeeId,
            session.ApprovedByEmployeeId,
            session.SubmittedOnUtc,
            session.CreatedOnUtc,
            session.CreatedBy,
            entries);
    }
}

