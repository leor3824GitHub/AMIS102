using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.SemiExpendablePropertyCard;

public sealed class GetSPCQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSPCQuery, SPCDto>
{
    public async ValueTask<SPCDto> Handle(GetSPCQuery query, CancellationToken cancellationToken)
    {
        // Verify the catalog item exists.
        var item = await dbContext.PropertyItemCatalog
            .Where(x => x.Id == query.ItemId)
            .Select(x => new { x.Id, x.Code, x.Name })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new NotFoundException($"Semi-expendable item with ID {query.ItemId} not found.");
        }

        // Collect all property IDs for this item type (needed for non-SMRR queries).
        var propertyIds = await dbContext.SemiExpendableProperties
            .IgnoreQueryFilters()
            .Where(x => x.ItemId == query.ItemId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<SPCEntryDto>();

        // ── 1. Receipts via SMRR ──────────────────────────────────────────────
        var smrrEntries = await (
            from smrrItem in dbContext.SMRRItems
                .Where(x => x.ItemId == query.ItemId)
            join smrr in dbContext.SuppliesMaterialsReceivingReports.IgnoreQueryFilters()
                on smrrItem.SMRRId equals smrr.Id
            where (!query.DateFrom.HasValue || smrr.Date >= query.DateFrom.Value)
               && (!query.DateTo.HasValue   || smrr.Date <= query.DateTo.Value)
            select new { smrr.Date, smrr.SMRRNo, smrrItem.Quantity, smrrItem.UnitCost, smrrItem.Description })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        entries.AddRange(smrrEntries.Select(x =>
            new SPCEntryDto(x.Date, "SMRR", x.SMRRNo, x.Quantity, 0, x.UnitCost, 0, x.Description)));

        if (propertyIds.Count > 0)
        {
            // ── 2. Issuances via ICS ──────────────────────────────────────────
            var icsRaw = await (
                from icsItem in dbContext.ICSItems.Where(x => propertyIds.Contains(x.SemiExpendablePropertyId))
                join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                    on icsItem.ICSId equals ics.Id
                where (!query.DateFrom.HasValue || ics.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || ics.Date <= query.DateTo.Value)
                select new { ics.Date, ics.ICSNo, icsItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(icsRaw
                .GroupBy(x => new { x.Date, x.ICSNo, x.UnitCost })
                .Select(g => new SPCEntryDto(g.Key.Date, "ICS", g.Key.ICSNo, 0, g.Count(), g.Key.UnitCost, 0, null)));

            // ── 3. Returns via RRSP ───────────────────────────────────────────
            var rrspRaw = await (
                from rrspItem in dbContext.RRSPItems.Where(x => propertyIds.Contains(x.SemiExpendablePropertyId))
                join rrsp in dbContext.ReceiptForReturnedProperties.IgnoreQueryFilters()
                    on rrspItem.RRSPId equals rrsp.Id
                where (!query.DateFrom.HasValue || rrsp.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || rrsp.Date <= query.DateTo.Value)
                select new { rrsp.Date, rrsp.RRSPNo, rrspItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(rrspRaw
                .GroupBy(x => new { x.Date, x.RRSPNo, x.UnitCost })
                .Select(g => new SPCEntryDto(g.Key.Date, "RRSP", g.Key.RRSPNo, g.Count(), 0, g.Key.UnitCost, 0, null)));

            // ── 4. Transfers via SMIR ─────────────────────────────────────────
            var smirRaw = await (
                from smirItem in dbContext.SMIRItems.Where(x => propertyIds.Contains(x.SemiExpendablePropertyId))
                join smir in dbContext.SemiExpendableIssuanceRecords.IgnoreQueryFilters()
                    on smirItem.SMIRId equals smir.Id
                where (!query.DateFrom.HasValue || smir.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || smir.Date <= query.DateTo.Value)
                select new { smir.Date, smir.SMIRNo, smirItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(smirRaw
                .GroupBy(x => new { x.Date, x.SMIRNo, x.UnitCost })
                .Select(g => new SPCEntryDto(g.Key.Date, "SMIR", g.Key.SMIRNo, 0, g.Count(), g.Key.UnitCost, 0, null)));

            // ── 5. Incidents via RLSDDSP ──────────────────────────────────────
            var pirRaw = await (
                from pirItem in dbContext.PropertyIncidentItems.Where(x => propertyIds.Contains(x.SemiExpendablePropertyId))
                join pir in dbContext.PropertyIncidentReports.IgnoreQueryFilters()
                    on pirItem.ReportId equals pir.Id
                where (!query.DateFrom.HasValue || pir.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || pir.Date <= query.DateTo.Value)
                select new { pir.Date, pir.ReportNo, pir.IncidentType, pirItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(pirRaw
                .GroupBy(x => new { x.Date, x.ReportNo, x.IncidentType, x.UnitCost })
                .Select(g => new SPCEntryDto(
                    g.Key.Date, "RLSDDSP", g.Key.ReportNo, 0, g.Count(), g.Key.UnitCost, 0,
                    g.Key.IncidentType.ToString())));

            // ── 6. Disposals via IIRUSP ───────────────────────────────────────
            var iiruспRaw = await (
                from iurItem in dbContext.UnserviceablePropertyItems.Where(x => propertyIds.Contains(x.SemiExpendablePropertyId))
                join iur in dbContext.UnserviceablePropertyReports.IgnoreQueryFilters()
                    on iurItem.ReportId equals iur.Id
                where (!query.DateFrom.HasValue || iur.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || iur.Date <= query.DateTo.Value)
                select new { iur.Date, iur.ReportNo, iur.DisposalMethod, iurItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(iiruспRaw
                .GroupBy(x => new { x.Date, x.ReportNo, x.DisposalMethod, x.UnitCost })
                .Select(g => new SPCEntryDto(
                    g.Key.Date, "IIRUSP", g.Key.ReportNo, 0, g.Count(), g.Key.UnitCost, 0,
                    g.Key.DisposalMethod.ToString())));
        }

        // Sort chronologically and compute running balance.
        entries.Sort((a, b) =>
        {
            var dateCmp = a.Date.CompareTo(b.Date);
            return dateCmp != 0 ? dateCmp : string.Compare(a.DocumentType, b.DocumentType, StringComparison.Ordinal);
        });

        int balance = 0;
        var finalEntries = entries
            .Select(e =>
            {
                balance += e.QuantityIn - e.QuantityOut;
                return e with { RunningBalance = balance };
            })
            .ToList();

        return new SPCDto(item.Id, item.Code, item.Name, finalEntries);
    }
}
