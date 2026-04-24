using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.PropertyHistory;

public sealed class GetPropertyHistoryQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPropertyHistoryQuery, PropertyHistoryDto>
{
    public async ValueTask<PropertyHistoryDto> Handle(
        GetPropertyHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var invItemId = query.TangibleInventoryItemId;

        // Header — load the inventory item joined to its catalog entry.
        var header = await (
            from inv in dbContext.TangibleInventoryItems.IgnoreQueryFilters()
            join catalog in dbContext.PropertyItemCatalog.IgnoreQueryFilters()
                on inv.ItemId equals catalog.Id
            where inv.Id == invItemId
            select new
            {
                inv.Id,
                inv.PropertyNo,
                inv.AssetType,
                inv.UnitCost,
                inv.ThresholdAmountUsed,
                inv.IsIssued,
                ItemCode = catalog.Code,
                ItemName = catalog.Name,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (header is null)
        {
            throw new NotFoundException($"Tangible inventory item with ID {invItemId} not found.");
        }

        var events = new List<PropertyHistoryEventDto>();

        // 1. Initial receipt — via parent TangibleInventory.
        var receiptEvent = await (
            from inv in dbContext.TangibleInventoryItems.IgnoreQueryFilters()
            join ti in dbContext.TangibleInventories.IgnoreQueryFilters()
                on inv.TangibleInventoryId equals ti.Id
            where inv.Id == invItemId
            select new { ti.Date, ti.ReportNo, ti.ReceivedFrom })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (receiptEvent is not null)
        {
            events.Add(new PropertyHistoryEventDto(
                receiptEvent.Date,
                "Received",
                "TangibleInventory",
                receiptEvent.ReportNo,
                $"Received from: {receiptEvent.ReceivedFrom}"));
        }

        // 2. ICS issuances (SE track).
        var icsEvents = await (
            from icsItem in dbContext.ICSItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                on icsItem.ICSId equals ics.Id
            orderby ics.Date
            select new { ics.Date, ics.ICSNo, ics.ReceivedByEmployeeId, ics.Status })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in icsEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                "Issued",
                "ICS",
                e.ICSNo,
                $"Issued to employee: {e.ReceivedByEmployeeId} | ICS status: {e.Status}"));
        }

        // 3. PAR issuances (PPE track).
        var parEvents = await (
            from parItem in dbContext.PARItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join par in dbContext.PropertyAcknowledgementReceipts.IgnoreQueryFilters()
                on parItem.PARId equals par.Id
            orderby par.Date
            select new { par.Date, par.PARNo, par.ReceivedByEmployeeId, par.PARType })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in parEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                "Issued",
                "PAR",
                e.PARNo,
                $"Issued to employee: {e.ReceivedByEmployeeId} | PAR type: {e.PARType}"));
        }

        // 4. PPEIR events (PPE depreciation / re-issuance records).
        var ppeirEvents = await (
            from ppeirItem in dbContext.PPEIRItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join ppeir in dbContext.PPEIssuanceReports.IgnoreQueryFilters()
                on ppeirItem.PPEIRId equals ppeir.Id
            orderby ppeir.Date
            select new
            {
                ppeir.Date,
                ppeir.PPEIRNo,
                ppeirItem.AccumulatedDepreciation,
                ppeirItem.BookValue,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in ppeirEvents)
        {
            var parts = new List<string>();
            if (e.AccumulatedDepreciation.HasValue)
                parts.Add($"Accum. Dep.: {e.AccumulatedDepreciation:N2}");
            if (e.BookValue.HasValue)
                parts.Add($"Book Value: {e.BookValue:N2}");
            var details = parts.Count > 0 ? string.Join(" | ", parts) : null;

            events.Add(new PropertyHistoryEventDto(e.Date, "Depreciation", "PPEIR", e.PPEIRNo, details));
        }

        // 5. RRSP returns (SE track).
        var rrspEvents = await (
            from rrspItem in dbContext.RRSPItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join rrsp in dbContext.ReceiptForReturnedProperties.IgnoreQueryFilters()
                on rrspItem.RRSPId equals rrsp.Id
            orderby rrsp.Date
            select new { rrsp.Date, rrsp.RRSPNo, rrsp.ReturnedByEmployeeId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in rrspEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                "Returned",
                "RRSP",
                e.RRSPNo,
                $"Returned by employee: {e.ReturnedByEmployeeId}"));
        }

        // 6. RRP returns (PPE track).
        var rrpEvents = await (
            from rrpItem in dbContext.RRPItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join rrp in dbContext.ReceiptsForReturnedPPE.IgnoreQueryFilters()
                on rrpItem.RRPId equals rrp.Id
            orderby rrp.Date
            select new { rrp.Date, rrp.RRPNo, rrp.ReturnedByEmployeeId, rrp.ReturnCategory })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in rrpEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                "Returned",
                "RRP",
                e.RRPNo,
                $"Returned by employee: {e.ReturnedByEmployeeId} | Category: {e.ReturnCategory}"));
        }

        // 7. SMIR transfers.
        var smirEvents = await (
            from smirItem in dbContext.SMIRItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join smir in dbContext.SemiExpendableIssuanceRecords.IgnoreQueryFilters()
                on smirItem.SMIRId equals smir.Id
            orderby smir.Date
            select new { smir.Date, smir.SMIRNo, smir.IssuanceType, smir.TransferredToOfficerName, smir.TransferredToTenantId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in smirEvents)
        {
            var details = e.IssuanceType.ToString();
            if (!string.IsNullOrWhiteSpace(e.TransferredToOfficerName))
            {
                details += $" | To: {e.TransferredToOfficerName}";
            }

            if (!string.IsNullOrWhiteSpace(e.TransferredToTenantId))
            {
                details += $" (TenantId: {e.TransferredToTenantId})";
            }

            events.Add(new PropertyHistoryEventDto(e.Date, "Transferred", "SMIR", e.SMIRNo, details));
        }

        // 8. Incident reports.
        var pirEvents = await (
            from pirItem in dbContext.PropertyIncidentItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join pir in dbContext.PropertyIncidentReports.IgnoreQueryFilters()
                on pirItem.ReportId equals pir.Id
            orderby pir.Date
            select new { pir.Date, pir.ReportNo, pir.IncidentType, pir.IncidentDetails })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in pirEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                e.IncidentType.ToString(),
                "RLSDDSP",
                e.ReportNo,
                e.IncidentDetails));
        }

        // 9. Unserviceable / disposal reports.
        var iurEvents = await (
            from iurItem in dbContext.UnserviceablePropertyItems.Where(x => x.TangibleInventoryItemId == invItemId)
            join iur in dbContext.UnserviceablePropertyReports.IgnoreQueryFilters()
                on iurItem.ReportId equals iur.Id
            orderby iur.Date
            select new { iur.Date, iur.ReportNo, iur.DisposalMethod, iurItem.ConditionRemarks })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var e in iurEvents)
        {
            events.Add(new PropertyHistoryEventDto(
                e.Date,
                "Disposed",
                "IIRUSP",
                e.ReportNo,
                $"Method: {e.DisposalMethod}" + (e.ConditionRemarks is null ? "" : $" | {e.ConditionRemarks}")));
        }

        // Determine current custodian from the most-recent active ICS/PAR (if still issued).
        Guid? currentCustodianId = null;
        if (header.IsIssued)
        {
            var latestIcs = await (
                from icsItem in dbContext.ICSItems.Where(x => x.TangibleInventoryItemId == invItemId)
                join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                    on icsItem.ICSId equals ics.Id
                orderby ics.Date descending
                select (Guid?)ics.ReceivedByEmployeeId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var latestPar = await (
                from parItem in dbContext.PARItems.Where(x => x.TangibleInventoryItemId == invItemId)
                join par in dbContext.PropertyAcknowledgementReceipts.IgnoreQueryFilters()
                    on parItem.PARId equals par.Id
                orderby par.Date descending
                select (Guid?)par.ReceivedByEmployeeId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            currentCustodianId = latestIcs ?? latestPar;
        }

        // Sort events chronologically; within same date, preserve document-no ordering.
        events.Sort((a, b) =>
        {
            var dateCmp = a.EventDate.CompareTo(b.EventDate);
            return dateCmp != 0 ? dateCmp : string.Compare(a.DocumentNo, b.DocumentNo, StringComparison.Ordinal);
        });

        return new PropertyHistoryDto(
            header.Id,
            header.PropertyNo,
            header.ItemCode,
            header.ItemName,
            header.AssetType.ToString(),
            header.UnitCost,
            header.ThresholdAmountUsed,
            header.IsIssued,
            currentCustodianId,
            events);
    }
}
