using AMIS.Maui.Data;
using AMIS.Maui.Data.Models;

namespace AMIS.Maui.Services;

// SQLite-backed offline cache for a session's coverage checklist. The list is what field staff need
// when they walk out of signal, so (per the MAUI caching rules) it is cached whole per session and
// served stale-while-revalidate: render the cache immediately, refresh from the API in the
// background, overwrite the cache.
public interface IPhysicalCountChecklistCache
{
    /// <summary>Cached checklist for a session plus when it was last written, or (empty, null) if none.</summary>
    Task<(List<PhysicalCountChecklistItemDto> Items, DateTimeOffset? CachedAt)> GetAsync(Guid sessionId);

    /// <summary>Overwrites the cached checklist for a session with a fresh API snapshot.</summary>
    Task SaveAsync(Guid sessionId, IEnumerable<PhysicalCountChecklistItemDto> items);
}

public sealed class PhysicalCountChecklistCache(LocalDb localDb) : IPhysicalCountChecklistCache
{
    public async Task<(List<PhysicalCountChecklistItemDto> Items, DateTimeOffset? CachedAt)> GetAsync(Guid sessionId)
    {
        var db = await localDb.GetConnectionAsync();
        var id = sessionId.ToString();
        var rows = await db.Table<CachedChecklistItem>()
            .Where(r => r.SessionId == id)
            .ToListAsync();

        if (rows.Count == 0)
            return ([], null);

        var items = rows.Select(r => new PhysicalCountChecklistItemDto(
            Guid.TryParse(r.AssetRegistryId, out var arId) ? arId : Guid.Empty,
            r.PropertyNo, r.AssetType, r.Description, r.Unit, r.UnitCost,
            Guid.TryParse(r.LocationId, out var locId) ? locId : null, r.LocationName,
            Guid.TryParse(r.CustodianId, out var custId) ? custId : null, r.AccountableOfficer,
            r.Status, r.Condition, r.ImageUrl)).ToList();

        return (items, rows.Min(r => r.CachedAt));
    }

    public async Task SaveAsync(Guid sessionId, IEnumerable<PhysicalCountChecklistItemDto> items)
    {
        var db = await localDb.GetConnectionAsync();
        var id = sessionId.ToString();
        var now = DateTimeOffset.UtcNow;

        // Employee-scoped overwrite: drop this session's rows, then insert the fresh snapshot.
        await db.Table<CachedChecklistItem>().Where(r => r.SessionId == id).DeleteAsync();

        var rows = items.Select(i => new CachedChecklistItem
        {
            LocalKey = $"{id}|{i.AssetRegistryId}",
            SessionId = id,
            AssetRegistryId = i.AssetRegistryId.ToString(),
            PropertyNo = i.PropertyNo,
            AssetType = i.AssetType,
            Description = i.Description,
            Unit = i.Unit,
            UnitCost = i.UnitCost,
            LocationId = i.LocationId?.ToString(),
            LocationName = i.LocationName,
            CustodianId = i.CustodianId?.ToString(),
            AccountableOfficer = i.AccountableOfficer,
            Status = i.Status,
            Condition = i.Condition,
            ImageUrl = i.ImageUrl,
            CachedAt = now,
        }).ToList();

        if (rows.Count > 0)
            await db.InsertAllAsync(rows);
    }
}
