using Playground.Maui.Data.Models;
using SQLite;

namespace Playground.Maui.Data;

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
            _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
            await _connection.CreateTableAsync<CachedEmployeeProfile>();
            await _connection.CreateTableAsync<CachedICS>();
            await _connection.CreateTableAsync<CachedPAR>();
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}
