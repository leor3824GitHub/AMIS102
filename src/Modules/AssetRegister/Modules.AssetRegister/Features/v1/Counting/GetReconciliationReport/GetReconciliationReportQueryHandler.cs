using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.GetReconciliationReport;

/// <summary>
/// Computes the Book-vs-Counted variance for a count session. Assets are unit-tracked,
/// so each row carries quantity 1 on the side it appears:
///   - recorded condition (good/repair/unserviceable) = Matched (1/1)
///   - Missing = Shortage (1/0)
///   - FoundAtStation = Overage (0/1)
///   - in-scope registry asset with no entry = Uncounted (1/0), a shortage candidate.
/// </summary>
public sealed class GetReconciliationReportQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReconciliationReportQuery, ReconciliationReportDto?>
{
    public async ValueTask<ReconciliationReportDto?> Handle(GetReconciliationReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var session = await db.PhysicalCountSessions.AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken).ConfigureAwait(false);
        if (session is null) return null;

        var rows = new List<ReconciliationRowDto>();
        foreach (var e in session.Entries)
        {
            var (rowStatus, bookQty, countedQty) = e.Condition switch
            {
                PhysicalCountCondition.Missing => (ReconciliationRowStatus.Shortage, 1, 0),
                PhysicalCountCondition.FoundAtStation => (ReconciliationRowStatus.Overage, 0, 1),
                _ => (ReconciliationRowStatus.Matched, 1, 1)
            };
            rows.Add(new ReconciliationRowDto(
                e.Id, e.AssetRegistryId, e.Snapshot?.PropertyNo,
                e.SnapshotArticle, e.SnapshotUnit, e.SnapshotUnitCost,
                bookQty, countedQty, rowStatus, e.Condition,
                e.NeedsRecount, e.RecountReason, e.LocationId, e.Remarks));
        }

        // Shortage candidates: in-scope registry assets never recorded on this session.
        var countedAssetIds = session.Entries
            .Where(e => e.AssetRegistryId is not null)
            .Select(e => e.AssetRegistryId!.Value)
            .ToHashSet();

        // PropertyNo is a value-converted VO (PropertyNumber ↔ string); EF can order/project the
        // converted property itself but not reach into its .Value, so read .Value client-side.
        var uncounted = await db.AssetRegistries.AsNoTracking()
            .InCountScope(session.FundCluster, session.Scope)
            .Where(a => !countedAssetIds.Contains(a.Id))
            .OrderBy(a => a.PropertyNo)
            .Select(a => new { a.Id, a.PropertyNo, a.Description, a.Unit, a.UnitCost, a.CurrentLocationId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        rows.AddRange(uncounted.Select(a => new ReconciliationRowDto(
            null, a.Id, a.PropertyNo.Value, a.Description, a.Unit, a.UnitCost,
            1, 0, ReconciliationRowStatus.Uncounted, null,
            false, null, a.CurrentLocationId, null)));

        return new ReconciliationReportDto(
            session.Id, session.Code, session.Status, session.FundCluster, session.Scope,
            session.OfficeOrderNo, session.FrozenOnUtc,
            rows,
            MatchedCount: rows.Count(r => r.RowStatus == ReconciliationRowStatus.Matched),
            ShortageCount: rows.Count(r => r.RowStatus == ReconciliationRowStatus.Shortage),
            OverageCount: rows.Count(r => r.RowStatus == ReconciliationRowStatus.Overage),
            UncountedCount: rows.Count(r => r.RowStatus == ReconciliationRowStatus.Uncounted),
            ShortageValue: rows.Where(r => r.RowStatus is ReconciliationRowStatus.Shortage or ReconciliationRowStatus.Uncounted).Sum(r => r.UnitCost),
            OverageValue: rows.Where(r => r.RowStatus == ReconciliationRowStatus.Overage).Sum(r => r.UnitCost));
    }
}
