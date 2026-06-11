using Playground.Maui.Data;
using Playground.Maui.Data.Models;

namespace Playground.Maui.Services;

public interface IPhysicalCountSyncService
{
    /// <summary>Attempts to send to API; if offline or failed, queues in SQLite. Returns true if synced live.</summary>
    Task<bool> RecordEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default);

    /// <summary>Attempts to send to API; if offline or failed, queues in SQLite. Returns true if synced live.</summary>
    Task<bool> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default);

    Task<int> GetPendingCountAsync();
    Task FlushPendingAsync(CancellationToken ct = default);
}

public sealed class PhysicalCountSyncService : IPhysicalCountSyncService
{
    private readonly LocalDb _localDb;
    private readonly IApiClient _apiClient;
    private int _isFlushing;

    public PhysicalCountSyncService(LocalDb localDb, IApiClient apiClient)
    {
        _localDb = localDb;
        _apiClient = apiClient;
    }

    public async Task<bool> RecordEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                await _apiClient.RecordPhysicalCountEntryAsync(sessionId, request, ct);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch { /* fall through to queue */ }
        }

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
        return false;
    }

    public async Task<bool> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                await _apiClient.AddFoundAtStationEntryAsync(sessionId, request, ct);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch { /* fall through to queue */ }
        }

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
            Remarks = request.Remarks,
            SyncStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return false;
    }

    public async Task<int> GetPendingCountAsync()
    {
        var db = await _localDb.GetConnectionAsync();
        return await db.Table<PendingCountEntry>()
            .Where(e => e.SyncStatus == "Pending")
            .CountAsync();
    }

    public async Task FlushPendingAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _isFlushing, 1) == 1) return;
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
                                entry.Remarks),
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
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    entry.SyncStatus = "Failed";
                    entry.LastError = ex.Message;
                    await db.UpdateAsync(entry);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isFlushing, 0);
        }
    }
}
