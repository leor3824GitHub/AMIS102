using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetICF;

public sealed class GetICFQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetICFQuery, ICFReportDto>
{
    public async ValueTask<ICFReportDto> Handle(GetICFQuery query, CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {query.SessionId} not found.");

        var entries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == query.SessionId)
            .OrderBy(x => x.PropertyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Per the ICF form: QuantityPerCard = 1 for individually tracked assets.
        // FoundAtStation entries have no card record, so perCard=0 and onHand=actual quantity found.
        // Shortage  = QuantityPerCard - QuantityOnHand when QuantityOnHand < QuantityPerCard
        // Overage   = QuantityOnHand - QuantityPerCard when QuantityOnHand > QuantityPerCard
        var lineItems = entries.Select((e, idx) =>
        {
            bool isFoundAtStation = e.Result == PhysicalCountEntryResult.FoundAtStation;
            int perCard = isFoundAtStation ? 0 : 1;
            int onHand  = (e.Result == PhysicalCountEntryResult.Found || isFoundAtStation)
                          ? e.QuantityOnHand : 0;
            int shortage   = Math.Max(0, perCard - onHand);
            int overage    = Math.Max(0, onHand - perCard);

            return new ICFLineItemDto(
                LineNo:         idx + 1,
                PropertyNumber: e.PropertyNumber,
                Description:    e.Description,
                UnitCost:       e.UnitCost,
                QuantityPerCard: perCard,
                QuantityOnHand:  onHand,
                Shortage:        shortage,
                Overage:         overage,
                Condition:       e.Condition?.ToString(),
                Remarks:         e.Remarks,
                Result:          e.Result?.ToString() ?? "Pending",
                IsScanned:       e.ScannedOnUtc.HasValue);
        }).ToList();

        return new ICFReportDto(
            session.Id,
            session.SessionNo,
            session.CountDate,
            session.StationOffice,
            session.Scope.ToString(),
            session.PreparedByEmployeeId,
            session.CertifiedByEmployeeId,
            session.ApprovedByEmployeeId,
            session.SubmittedOnUtc,
            lineItems);
    }
}

