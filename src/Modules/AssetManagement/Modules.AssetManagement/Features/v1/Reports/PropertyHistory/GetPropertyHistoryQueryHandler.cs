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
        var propId = query.SemiExpendablePropertyId;

        // Load the property (no query filter — may be disposed/transferred).
        var property = await dbContext.SemiExpendableProperties
            .IgnoreQueryFilters()
            .Include(x => x.Item)
            .Where(x => x.Id == propId)
            .Select(x => new
            {
                x.Id,
                x.PropertyNo,
                x.SerialNo,
                x.Category,
                x.UnitCost,
                x.Status,
                x.CurrentCustodianId,
                ItemCode = x.Item.Code,
                ItemName = x.Item.Name,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
        {
            throw new NotFoundException($"Semi-expendable property with ID {propId} not found.");
        }

        var events = new List<PropertyHistoryEventDto>();

        // 1. SMRR receipt (the property's origin document).
        var smrrEvent = await (
            from smrrItem in dbContext.SMRRItems
                .Where(x => dbContext.SemiExpendableProperties
                    .IgnoreQueryFilters()
                    .Any(p => p.Id == propId && p.SMRRItemId == x.Id))
            join smrr in dbContext.SuppliesMaterialsReceivingReports.IgnoreQueryFilters()
                on smrrItem.SMRRId equals smrr.Id
            select new { smrr.Date, smrr.SMRRNo, smrr.ReceivedFrom })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (smrrEvent is not null)
        {
            events.Add(new PropertyHistoryEventDto(
                smrrEvent.Date,
                "Received",
                "SMRR",
                smrrEvent.SMRRNo,
                $"Received from: {smrrEvent.ReceivedFrom}"));
        }

        // 2. ICS issuances (all, including historical).
        var icsEvents = await (
            from icsItem in dbContext.ICSItems.Where(x => x.SemiExpendablePropertyId == propId)
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

        // 3. RRSP returns.
        var rrspEvents = await (
            from rrspItem in dbContext.RRSPItems.Where(x => x.SemiExpendablePropertyId == propId)
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

        // 4. SMIR transfers.
        var smirEvents = await (
            from smirItem in dbContext.SMIRItems.Where(x => x.SemiExpendablePropertyId == propId)
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

        // 5. RLSDDSP incidents.
        var pirEvents = await (
            from pirItem in dbContext.PropertyIncidentItems.Where(x => x.SemiExpendablePropertyId == propId)
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

        // 6. IIRUSP disposals.
        var iurEvents = await (
            from iurItem in dbContext.UnserviceablePropertyItems.Where(x => x.SemiExpendablePropertyId == propId)
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

        // Sort events chronologically; within same date preserve document-type order.
        events.Sort((a, b) =>
        {
            var dateCmp = a.EventDate.CompareTo(b.EventDate);
            return dateCmp != 0 ? dateCmp : string.Compare(a.DocumentNo, b.DocumentNo, StringComparison.Ordinal);
        });

        return new PropertyHistoryDto(
            property.Id,
            property.PropertyNo,
            property.ItemCode,
            property.ItemName,
            property.SerialNo,
            property.Category.ToString(),
            property.UnitCost,
            property.Status.ToString(),
            property.CurrentCustodianId,
            events);
    }
}
