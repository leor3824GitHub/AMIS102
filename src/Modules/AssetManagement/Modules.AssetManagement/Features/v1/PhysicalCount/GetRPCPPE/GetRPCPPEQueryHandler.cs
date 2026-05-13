using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCPPE;

public sealed class GetRPCPPEQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRPCPPEQuery, RPCPPEReportDto>
{
    public async ValueTask<RPCPPEReportDto> Handle(GetRPCPPEQuery query, CancellationToken cancellationToken)
    {
        var session = await dbContext.PhysicalCountSessions
            .FirstOrDefaultAsync(x => x.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new NotFoundException($"Physical count session {query.SessionId} not found.");

        // RPCPPE covers PPE items only (scope filter)
        if (session.Scope == PhysicalCountScope.SemiExpendableOnly)
            throw new CustomException("RPCPPE is only applicable to sessions that include PPE items.", Array.Empty<string>(), HttpStatusCode.UnprocessableEntity);

        // Load all entries for this session up front. We'll classify them in memory
        // — avoids LINQ-to-SQL join quirks with nullable Guid foreign keys.
        var sessionEntries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == query.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linkedInvIds = sessionEntries
            .Where(e => e.TangibleInventoryItemId.HasValue)
            .Select(e => e.TangibleInventoryItemId!.Value)
            .Distinct()
            .ToList();

        // Look up PropertyCode + AcquisitionDate + AssetType for every linked inventory item.
        var invInfo = linkedInvIds.Count == 0
            ? new Dictionary<Guid, (string PropertyNo, DateOnly AcquisitionDate, AssetType AssetType)>()
            : (await dbContext.TangibleInventoryItems
                .Where(inv => linkedInvIds.Contains(inv.Id))
                .Select(inv => new { inv.Id, inv.PropertyNo, inv.AcquisitionDate, inv.AssetType })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => (x.PropertyNo, x.AcquisitionDate, x.AssetType));

        // RPCPPE eligibility:
        //   1. Linked to a PPE TangibleInventoryItem  → standard checklist row, OR
        //   2. Found-at-Station entry (not linked)    → discovered during the count.
        // For PPEOnly sessions every linked entry is PPE by construction; for Both
        // scope sessions we must check AssetType == PPE.
        var allEntries = sessionEntries
            .Select(e =>
            {
                if (e.TangibleInventoryItemId.HasValue
                    && invInfo.TryGetValue(e.TangibleInventoryItemId.Value, out var info)
                    && info.AssetType == AssetType.PPE)
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

        var invItemIds = allEntries
            .Where(e => e.Entry.TangibleInventoryItemId.HasValue)
            .Select(e => e.Entry.TangibleInventoryItemId!.Value)
            .Distinct()
            .ToList();

        // Load most-recent depreciation per inventory item from PPEIRItems.
        var allDeprRows = await (
            from item in dbContext.PPEIRItems
            where invItemIds.Contains(item.TangibleInventoryItemId) && item.AccumulatedDepreciation.HasValue
            join rpt in dbContext.PPEIssuanceReports on item.PPEIRId equals rpt.Id
            orderby rpt.Date descending
            select new
            {
                item.TangibleInventoryItemId,
                item.AccumulatedDepreciation,
                item.BookValue,
                rpt.Date,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var depreciationData = allDeprRows
            .GroupBy(x => x.TangibleInventoryItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var lineItems = allEntries.Select((row, idx) =>
        {
            var e = row.Entry;
            bool isFoundAtStation = e.Result == PhysicalCountEntryResult.FoundAtStation;
            int perCard = isFoundAtStation ? 0 : 1;
            int onHand  = (e.Result == PhysicalCountEntryResult.Found || isFoundAtStation)
                          ? e.QuantityOnHand : 0;
            int shortage = Math.Max(0, perCard - onHand);
            int overage  = Math.Max(0, onHand - perCard);

            depreciationData.TryGetValue(e.TangibleInventoryItemId ?? Guid.Empty, out var depr);

            return new RPCPPELineItemDto(
                LineNo:                  idx + 1,
                PropertyCode:            row.PropertyCode ?? string.Empty,
                Description:             e.Description,
                PropertyNumber:          e.PropertyNumber,
                DateAcquired:            row.AcquisitionDate ?? session.CountDate,
                UnitCost:                e.UnitCost,
                AccumulatedDepreciation: depr?.AccumulatedDepreciation,
                BookValue:               depr?.BookValue,
                QuantityPerCard:         perCard,
                QuantityOnHand:          onHand,
                Shortage:                shortage,
                Overage:                 overage,
                Condition:               e.Condition?.ToString(),
                Remarks:                 e.Remarks,
                Result:                  e.Result?.ToString() ?? "Pending",
                IsScanned:               e.ScannedOnUtc.HasValue);
        }).ToList();

        var summary = new RPCPPESummaryDto(
            TotalItems:                  lineItems.Count,
            Found:                       lineItems.Count(x => x.Result == "Found"),
            NotFound:                    lineItems.Count(x => x.Result == "NotFound"),
            FoundAtStation:              lineItems.Count(x => x.Result == "FoundAtStation"),
            Pending:                     lineItems.Count(x => x.Result == "Pending"),
            TotalUnitCost:               lineItems.Sum(x => x.UnitCost),
            TotalAccumulatedDepreciation: lineItems.Any(x => x.AccumulatedDepreciation.HasValue)
                                            ? lineItems.Sum(x => x.AccumulatedDepreciation ?? 0)
                                            : null,
            TotalBookValue:              lineItems.Any(x => x.BookValue.HasValue)
                                            ? lineItems.Sum(x => x.BookValue ?? 0)
                                            : null,
            TotalShortage:               lineItems.Sum(x => x.Shortage),
            TotalOverage:                lineItems.Sum(x => x.Overage));

        return new RPCPPEReportDto(
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
