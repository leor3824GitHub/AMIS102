using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.SemiExpendablePropertyCard;

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

        // Collect all SE inventory-item IDs for this catalog type (SPC card = SE only).
        var invItemIds = await dbContext.TangibleInventoryItems
            .Where(x => x.ItemId == query.ItemId && x.AssetType == AssetType.SE)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<SPCEntryDto>();

        if (invItemIds.Count > 0)
        {
            // ── 1. Receipts via TangibleInventory ─────────────────────────────
            var receiptEntries = await (
                from inv in dbContext.TangibleInventoryItems
                    .Where(x => invItemIds.Contains(x.Id))
                join ti in dbContext.TangibleInventories
                    on inv.TangibleInventoryId equals ti.Id
                where (!query.DateFrom.HasValue || ti.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || ti.Date <= query.DateTo.Value)
                select new { ti.Date, ti.ReportNo, inv.Quantity, inv.UnitCost, inv.Description })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(receiptEntries.Select(x =>
                new SPCEntryDto(x.Date, "TangibleInventory", x.ReportNo, x.Quantity, 0, x.UnitCost, 0, x.Description)));

            // ── 2. Issuances via ICS ──────────────────────────────────────────
            var icsRaw = await (
                from icsItem in dbContext.ICSItems.Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
                join ics in dbContext.InventoryCustodianSlips
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
                from rrspItem in dbContext.RRSPItems.Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
                join rrsp in dbContext.ReceiptForReturnedProperties
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
                from smirItem in dbContext.SMIRItems.Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
                join smir in dbContext.SemiExpendableIssuanceRecords
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
                from pirItem in dbContext.PropertyIncidentItems.Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
                join pir in dbContext.PropertyIncidentReports
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
            var iuruspRaw = await (
                from iurItem in dbContext.UnserviceablePropertyItems.Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
                join iur in dbContext.UnserviceablePropertyReports
                    on iurItem.ReportId equals iur.Id
                where (!query.DateFrom.HasValue || iur.Date >= query.DateFrom.Value)
                   && (!query.DateTo.HasValue   || iur.Date <= query.DateTo.Value)
                select new { iur.Date, iur.ReportNo, iur.DisposalMethod, iurItem.UnitCost })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            entries.AddRange(iuruspRaw
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

