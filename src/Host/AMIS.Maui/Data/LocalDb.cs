using AMIS.Maui.Data.Models;
using SQLite;

namespace AMIS.Maui.Data;

// CA1001: registered as a DI singleton, so _lock and the SQLite connection live for the whole process
// and are reclaimed at exit. Making this IDisposable would imply a teardown point that does not exist.
#pragma warning disable CA1001
public sealed class LocalDb
#pragma warning restore CA1001
{
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync();
        try
        {
            if (_connection is not null)
                return _connection;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "amis-cache.db");
            _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite);
            await _connection.CreateTableAsync<PendingCountEntry>();
            await _connection.CreateTableAsync<CachedChecklistItem>();
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Wipes all locally cached data. Called on logout so the next user starts clean.
    /// Note: this also discards any unsynced <see cref="PendingCountEntry"/> rows — callers
    /// should warn the user when pending entries exist before invoking this.
    /// </summary>
    public async Task ClearAllAsync()
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<PendingCountEntry>();
        await db.DeleteAllAsync<CachedChecklistItem>();
    }
}
