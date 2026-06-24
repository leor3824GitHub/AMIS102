using AMIS.Maui.Data.Models;
using SQLite;

namespace AMIS.Maui.Data;

public sealed class LocalDb
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
    }
}
