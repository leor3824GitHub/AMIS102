using AMIS.Maui.Data;
using AMIS.Maui.Data.Models;

namespace AMIS.Maui.Services;

// Outcome of a single record/add attempt. Distinguishes a genuine offline queue from a server
// rejection so the UI can tell the operator the truth instead of a misleading "added offline".
public enum RecordSaveStatus
{
    SavedToServer,
    QueuedOffline,
    Rejected
}

public readonly record struct RecordSaveResult(RecordSaveStatus Status, string? Error = null);

// Result of draining the local queue.
public sealed record FlushResult(int Sent, int Failed, string? LastError = null);

public interface IPhysicalCountSyncService
{
    /// <summary>Saves to the server when online; queues in SQLite only when genuinely offline.
    /// A server rejection is surfaced (never silently queued).</summary>
    Task<RecordSaveResult> RecordEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default);

    /// <summary>Saves to the server when online; queues in SQLite only when genuinely offline.
    /// A server rejection is surfaced (never silently queued).</summary>
    Task<RecordSaveResult> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default);

    Task<int> GetPendingCountAsync();
    Task<int> GetFailedCountAsync();

    /// <summary>Not-yet-synced entries (queued or failed) for a session, so the review screen can show them.</summary>
    Task<List<PendingCountEntry>> GetUnsyncedEntriesAsync(Guid sessionId);

    Task DiscardFailedAsync();
    Task<FlushResult> FlushPendingAsync(CancellationToken ct = default);
}

public sealed class PhysicalCountSyncService : IPhysicalCountSyncService
{
    private readonly LocalDb _localDb;
    private readonly IApiClient _apiClient;
    // Process-wide single-flight guard: the service is Transient, so a per-instance field wouldn't stop
    // two pages' instances from replaying the same queued rows at once (duplicate inserts).
    private static int _isFlushing;

    public PhysicalCountSyncService(LocalDb localDb, IApiClient apiClient)
    {
        _localDb = localDb;
        _apiClient = apiClient;
    }

    public async Task<RecordSaveResult> RecordEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                await _apiClient.RecordPhysicalCountEntryAsync(sessionId, request, ct);
                return new RecordSaveResult(RecordSaveStatus.SavedToServer);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException ex) when (ex.StatusCode is not null)
            {
                // The server answered with an error (validation / conflict / etc.). Surface it —
                // do NOT queue, or the operator would think it saved when it never will.
                return new RecordSaveResult(RecordSaveStatus.Rejected, ex.Message);
            }
            catch
            {
                // Transport failure (socket/DNS/timeout) despite a reported connection — fall through to queue.
            }
        }

        await QueueRecordAsync(sessionId, request);
        return new RecordSaveResult(RecordSaveStatus.QueuedOffline);
    }

    public async Task<RecordSaveResult> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                await _apiClient.AddFoundAtStationEntryAsync(sessionId, request, ct);
                return new RecordSaveResult(RecordSaveStatus.SavedToServer);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException ex) when (ex.StatusCode is not null)
            {
                return new RecordSaveResult(RecordSaveStatus.Rejected, ex.Message);
            }
            catch
            {
                // Transport failure — fall through to queue.
            }
        }

        await QueueFoundAtStationAsync(sessionId, request);
        return new RecordSaveResult(RecordSaveStatus.QueuedOffline);
    }

    private async Task QueueRecordAsync(Guid sessionId, RecordCountEntryRequest request)
    {
        var db = await _localDb.GetConnectionAsync();
        await db.InsertAsync(new PendingCountEntry
        {
            SessionId = sessionId.ToString(),
            AssetRegistryId = request.AssetRegistryId.ToString(),
            LocationId = request.LocationId.ToString(),
            Article = request.Article,
            Unit = request.Unit,
            UnitCost = request.UnitCost,
            Condition = request.Condition,
            Remarks = request.Remarks,
            IsScanned = request.IsScanned,
            IsFoundAtStation = false,
            SyncStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    private async Task QueueFoundAtStationAsync(Guid sessionId, AddFoundAtStationRequest request)
    {
        var db = await _localDb.GetConnectionAsync();
        await db.InsertAsync(new PendingCountEntry
        {
            SessionId = sessionId.ToString(),
            PropertyNumber = request.PropertyNumber,
            LocationId = request.LocationId.ToString(),
            Article = request.Description,
            Unit = request.Unit,
            UnitCost = request.UnitCost,
            IsFoundAtStation = true,
            ProposedCatalogItemId = request.ProposedCatalogItemId?.ToString(),
            Remarks = request.Remarks,
            SyncStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    public async Task<int> GetPendingCountAsync()
    {
        var db = await _localDb.GetConnectionAsync();
        return await db.Table<PendingCountEntry>()
            .Where(e => e.SyncStatus == "Pending")
            .CountAsync();
    }

    public async Task<int> GetFailedCountAsync()
    {
        var db = await _localDb.GetConnectionAsync();
        return await db.Table<PendingCountEntry>()
            .Where(e => e.SyncStatus == "Failed")
            .CountAsync();
    }

    public async Task<List<PendingCountEntry>> GetUnsyncedEntriesAsync(Guid sessionId)
    {
        var db = await _localDb.GetConnectionAsync();
        var id = sessionId.ToString();
        return await db.Table<PendingCountEntry>()
            .Where(e => e.SessionId == id)
            .ToListAsync();
    }

    public async Task DiscardFailedAsync()
    {
        var db = await _localDb.GetConnectionAsync();
        var failed = await db.Table<PendingCountEntry>()
            .Where(e => e.SyncStatus == "Failed")
            .ToListAsync();
        foreach (var entry in failed)
            await db.DeleteAsync(entry);
    }

    public async Task<FlushResult> FlushPendingAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _isFlushing, 1) == 1) return new FlushResult(0, 0);
        var sent = 0;
        var failed = 0;
        string? lastError = null;
        try
        {
            var db = await _localDb.GetConnectionAsync();
            var pending = await db.Table<PendingCountEntry>()
                .Where(e => e.SyncStatus == "Pending")
                .ToListAsync();

            foreach (var entry in pending)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    if (entry.IsFoundAtStation)
                    {
                        await _apiClient.AddFoundAtStationEntryAsync(
                            Guid.Parse(entry.SessionId),
                            new AddFoundAtStationRequest(
                                entry.PropertyNumber,
                                entry.Article,
                                entry.Unit,
                                entry.UnitCost,
                                Guid.Parse(entry.LocationId),
                                entry.Remarks,
                                Guid.TryParse(entry.ProposedCatalogItemId, out var catId) ? catId : null),
                            ct);
                    }
                    else
                    {
                        await _apiClient.RecordPhysicalCountEntryAsync(
                            Guid.Parse(entry.SessionId),
                            new RecordCountEntryRequest(
                                Guid.Parse(entry.AssetRegistryId!),
                                entry.Article,
                                entry.Unit,
                                entry.UnitCost,
                                entry.Condition ?? "InGoodCondition",
                                Guid.Parse(entry.LocationId),
                                entry.Remarks,
                                entry.IsScanned),
                            ct);
                    }
                    await db.DeleteAsync(entry);
                    sent++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    entry.SyncStatus = "Failed";
                    entry.LastError = ex.Message;
                    await db.UpdateAsync(entry);
                    failed++;
                    lastError = ex.Message;
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isFlushing, 0);
        }

        return new FlushResult(sent, failed, lastError);
    }
}
