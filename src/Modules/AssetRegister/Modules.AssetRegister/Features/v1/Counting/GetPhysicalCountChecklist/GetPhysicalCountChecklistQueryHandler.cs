using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.GetPhysicalCountChecklist;

/// <summary>
/// Builds a session's coverage checklist: every in-scope, on-the-books asset (same fund cluster +
/// scope as reconciliation, via <see cref="CountScopeQuery.InCountScope"/>) tagged with its count
/// status. This is the mobile "what's left to find" worklist — the read-only inverse of the
/// record-as-you-go scan screen. Location and accountable-officer names are resolved so the client
/// can filter/group and cache the whole set offline without further round-trips.
/// </summary>
public sealed class GetPhysicalCountChecklistQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPhysicalCountChecklistQuery, PhysicalCountChecklistDto?>
{
    public async ValueTask<PhysicalCountChecklistDto?> Handle(GetPhysicalCountChecklistQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var session = await db.PhysicalCountSessions.AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken).ConfigureAwait(false);
        if (session is null) return null;

        // Recorded status per known asset. A real count (good/repair/unserviceable) wins over a
        // Missing marker on the same asset; FoundAtStation entries carry no AssetRegistryId so they
        // never appear here (they aren't part of the expected universe).
        var entryByAsset = session.Entries
            .Where(e => e.AssetRegistryId is not null)
            .GroupBy(e => e.AssetRegistryId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(e => e.Condition != PhysicalCountCondition.Missing
                                         && e.Condition != PhysicalCountCondition.FoundAtStation)
                      ?? g.First()).Condition);

        // In-scope universe. PropertyNo is a value-converted VO — project the VO and read .Value
        // client-side (EF can't reach into .Value), same as the reconciliation report.
        var assets = await db.AssetRegistries.AsNoTracking()
            .InCountScope(session.FundCluster, session.Scope)
            .OrderBy(a => a.PropertyNo)
            .Select(a => new
            {
                a.Id,
                a.PropertyNo,
                a.AssetType,
                a.Description,
                a.Unit,
                a.UnitCost,
                a.CurrentLocationId,
                a.CurrentCustodianId,
                a.CurrentAccountabilityId,
                // Presence flag only (IS NOT NULL) — the photo bytes never enter this mobile payload.
                HasImage = a.ImageUrl != null,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Resolve location + accountable-officer names in two set-based lookups (no N+1).
        var locationIds = assets.Where(a => a.CurrentLocationId is not null)
            .Select(a => a.CurrentLocationId!.Value).Distinct().ToList();
        var locationNames = locationIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Locations.AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .Select(l => new { l.Id, l.Name })
                .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken).ConfigureAwait(false);

        var accountabilityIds = assets.Where(a => a.CurrentAccountabilityId is not null)
            .Select(a => a.CurrentAccountabilityId!.Value).Distinct().ToList();
        var officerNames = accountabilityIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.PropertyAccountabilities.AsNoTracking()
                .Where(p => accountabilityIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ReceivedBy.PrintedName })
                .ToDictionaryAsync(p => p.Id, p => p.PrintedName, cancellationToken).ConfigureAwait(false);

        var items = new List<PhysicalCountChecklistItemDto>(assets.Count);
        foreach (var a in assets)
        {
            string status;
            PhysicalCountCondition? condition = null;
            if (entryByAsset.TryGetValue(a.Id, out var recorded))
            {
                condition = recorded;
                status = recorded == PhysicalCountCondition.Missing ? "Missing" : "Counted";
            }
            else
            {
                status = "Uncounted";
            }

            var locationName = a.CurrentLocationId is Guid lid && locationNames.TryGetValue(lid, out var ln) ? ln : null;
            var officerName = a.CurrentAccountabilityId is Guid aid && officerNames.TryGetValue(aid, out var on) ? on : null;

            items.Add(new PhysicalCountChecklistItemDto(
                a.Id, a.PropertyNo.Value, a.AssetType, a.Description, a.Unit, a.UnitCost,
                a.CurrentLocationId, locationName, a.CurrentCustodianId, officerName, status, condition,
                a.HasImage));
        }

        return new PhysicalCountChecklistDto(
            session.Id, session.Code, session.Scope, session.Status, session.FundCluster,
            TotalCount: items.Count,
            CountedCount: items.Count(i => i.Status == "Counted"),
            MissingCount: items.Count(i => i.Status == "Missing"),
            UncountedCount: items.Count(i => i.Status == "Uncounted"),
            Items: items);
    }
}
