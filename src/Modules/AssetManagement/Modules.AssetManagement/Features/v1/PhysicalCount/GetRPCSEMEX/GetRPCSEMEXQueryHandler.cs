using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCSEMEX;

public sealed class GetRPCSEMEXQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRPCSEMEXQuery, RPCSEMEXReportDto>
{
    public async ValueTask<RPCSEMEXReportDto> Handle(GetRPCSEMEXQuery query, CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {query.SessionId} not found.");

        // RPCSEMEX covers SE items only — invalid for PPE-only sessions.
        if (session.Scope == PhysicalCountScope.PPEOnly)
            throw new CustomException("RPCSEMEX is only applicable to sessions that include Semi-Expendable items.", Array.Empty<string>(), HttpStatusCode.UnprocessableEntity);

        // Load all entries for this session, then classify in memory to avoid
        // LINQ-to-SQL join quirks with nullable foreign keys.
        var sessionEntries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == query.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linkedInvIds = sessionEntries
            .Where(e => e.TangibleInventoryItemId.HasValue)
            .Select(e => e.TangibleInventoryItemId!.Value)
            .Distinct()
            .ToList();

        var invInfo = linkedInvIds.Count == 0
            ? new Dictionary<Guid, (string PropertyNo, DateOnly AcquisitionDate, AssetType AssetType)>()
            : (await dbContext.TangibleInventoryItems
                .Where(inv => linkedInvIds.Contains(inv.Id))
                .Select(inv => new { inv.Id, inv.PropertyNo, inv.AcquisitionDate, inv.AssetType })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => (x.PropertyNo, x.AcquisitionDate, x.AssetType));

        // RPCSEMEX eligibility:
        //   1. Linked to a SE TangibleInventoryItem  → standard checklist row, OR
        //   2. Found-at-Station entry (not linked)   → discovered during the count.
        // For SemiExpendableOnly sessions every linked entry is SE by construction;
        // for Both scope we check AssetType == SE.
        var allEntries = sessionEntries
            .Select(e =>
            {
                if (e.TangibleInventoryItemId.HasValue
                    && invInfo.TryGetValue(e.TangibleInventoryItemId.Value, out var info)
                    && info.AssetType == AssetType.SE)
                {
                    return new { Entry = e, PropertyCode = (string?)info.PropertyNo, AcquisitionDate = (DateOnly?)info.AcquisitionDate, Include = true };
                }

                if (e.TangibleInventoryItemId == null && e.Result == PhysicalCountEntryResult.FoundAtStation)
                {
                    return new { Entry = e, PropertyCode = (string?)null, AcquisitionDate = (DateOnly?)null, Include = true };
                }

                return new { Entry = e, PropertyCode = (string?)null, AcquisitionDate = (DateOnly?)null, Include = false };
            })
            .Where(x => x.Include)
            .OrderBy(r => r.Entry.PropertyNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lineItems = allEntries.Select((row, idx) =>
        {
            var e = row.Entry;
            bool isFoundAtStation = e.Result == PhysicalCountEntryResult.FoundAtStation;
            int perCard = isFoundAtStation ? 0 : 1;
            int onHand  = (e.Result == PhysicalCountEntryResult.Found || isFoundAtStation)
                          ? e.QuantityOnHand : 0;
            int shortage = Math.Max(0, perCard - onHand);
            int overage  = Math.Max(0, onHand - perCard);

            return new RPCSEMEXLineItemDto(
                LineNo:          idx + 1,
                PropertyCode:    row.PropertyCode ?? string.Empty,
                Description:     e.Description,
                PropertyNumber:  e.PropertyNumber,
                DateAcquired:    row.AcquisitionDate ?? session.CountDate,
                UnitCost:        e.UnitCost,
                QuantityPerCard: perCard,
                QuantityOnHand:  onHand,
                Shortage:        shortage,
                Overage:         overage,
                Condition:       e.Condition?.ToString(),
                Remarks:         e.Remarks,
                Result:          e.Result?.ToString() ?? "Pending",
                IsScanned:       e.ScannedOnUtc.HasValue);
        }).ToList();

        var summary = new RPCSEMEXSummaryDto(
            TotalItems:     lineItems.Count,
            Found:          lineItems.Count(x => x.Result == "Found"),
            NotFound:       lineItems.Count(x => x.Result == "NotFound"),
            FoundAtStation: lineItems.Count(x => x.Result == "FoundAtStation"),
            Pending:        lineItems.Count(x => x.Result == "Pending"),
            TotalUnitCost:  lineItems.Sum(x => x.UnitCost),
            TotalShortage:  lineItems.Sum(x => x.Shortage),
            TotalOverage:   lineItems.Sum(x => x.Overage));

        return new RPCSEMEXReportDto(
            session.Id,
            session.SessionNo,
            session.CountDate,
            session.StationOffice,
            session.PreparedByEmployeeId,
            session.CertifiedByEmployeeId,
            session.ApprovedByEmployeeId,
            session.SubmittedOnUtc,
            lineItems,
            summary);
    }
}
