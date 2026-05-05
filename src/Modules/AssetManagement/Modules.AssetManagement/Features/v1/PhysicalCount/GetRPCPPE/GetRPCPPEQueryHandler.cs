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

        // Load PPE checklist entries — join to TangibleInventoryItem to filter AssetType == PPE.
        var ppeEntries = await (
            from e in dbContext.PhysicalCountEntries.Where(x => x.SessionId == query.SessionId && x.TangibleInventoryItemId != null)
            join inv in dbContext.TangibleInventoryItems
                on e.TangibleInventoryItemId equals inv.Id
            where inv.AssetType == AssetType.PPE
            orderby e.PropertyNumber
            select new
            {
                Entry = e,
                inv.PropertyNo,
                inv.AcquisitionDate,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var invItemIds = ppeEntries.Select(e => e.Entry.TangibleInventoryItemId!.Value).Distinct().ToList();

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

        var lineItems = ppeEntries.Select((row, idx) =>
        {
            var e = row.Entry;
            bool isFoundAtStation = e.Result == PhysicalCountEntryResult.FoundAtStation;
            int perCard = isFoundAtStation ? 0 : 1;
            int onHand  = (e.Result == PhysicalCountEntryResult.Found || isFoundAtStation)
                          ? e.QuantityOnHand : 0;
            int shortage = Math.Max(0, perCard - onHand);
            int overage  = Math.Max(0, onHand - perCard);

            depreciationData.TryGetValue(e.TangibleInventoryItemId!.Value, out var depr);

            return new RPCPPELineItemDto(
                LineNo:                  idx + 1,
                PropertyCode:            row.PropertyNo,
                Description:             e.Description,
                PropertyNumber:          e.PropertyNumber,
                DateAcquired:            row.AcquisitionDate,
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
