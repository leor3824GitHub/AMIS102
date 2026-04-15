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
            throw new KeyNotFoundException($"Physical count session {query.SessionId} not found.");

        // RPCPPE covers PPE items only (scope filter)
        if (session.Scope == PhysicalCountScope.SemiExpendableOnly)
            throw new InvalidOperationException("RPCPPE is only applicable to sessions that include PPE items.");

        // Load PPE entries from the session checklist
        var ppeEntries = await dbContext.PhysicalCountEntries
            .Where(x => x.SessionId == query.SessionId && x.PPEItemId != null)
            .OrderBy(x => x.PropertyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Load PPE item details (PropertyCode, DateAcquired) for the entries
        var ppeItemIds = ppeEntries
            .Select(e => e.PPEItemId!.Value)
            .Distinct()
            .ToList();

        var ppeItemDetails = await dbContext.PPEItems
            .Where(x => ppeItemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PropertyCode, x.DateAcquired })
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        // Load the most recent depreciation data per PPE item from PPEIRItems.
        // Join with PPEIssuanceReports to order by issuance date (not by accumulated value,
        // which would be wrong when depreciation is partially filled or corrected).
        var allDeprRows = await dbContext.PPEIRItems
            .Where(x => ppeItemIds.Contains(x.PPEItemId) && x.AccumulatedDepreciation.HasValue)
            .Join(dbContext.PPEIssuanceReports,
                item => item.PPEIRId,
                rpt  => rpt.Id,
                (item, rpt) => new
                {
                    item.PPEItemId,
                    item.AccumulatedDepreciation,
                    item.BookValue,
                    rpt.Date,
                })
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // In-memory: pick the first (most-recent) row per PPE item
        var depreciationData = allDeprRows
            .GroupBy(x => x.PPEItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var lineItems = ppeEntries.Select((e, idx) =>
        {
            bool isFoundAtStation = e.Result == PhysicalCountEntryResult.FoundAtStation;
            int perCard = isFoundAtStation ? 0 : 1;
            int onHand  = (e.Result == PhysicalCountEntryResult.Found || isFoundAtStation)
                          ? e.QuantityOnHand : 0;
            int shortage = Math.Max(0, perCard - onHand);
            int overage  = Math.Max(0, onHand - perCard);

            ppeItemDetails.TryGetValue(e.PPEItemId!.Value, out var ppe);
            depreciationData.TryGetValue(e.PPEItemId!.Value, out var depr);

            return new RPCPPELineItemDto(
                LineNo:                  idx + 1,
                PropertyCode:            ppe?.PropertyCode ?? string.Empty,
                Description:             e.Description,
                PropertyNumber:          e.PropertyNumber,
                DateAcquired:            ppe?.DateAcquired ?? default,
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
