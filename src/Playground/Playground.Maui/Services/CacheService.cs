using Playground.Maui.Data;
using Playground.Maui.Data.Models;

namespace Playground.Maui.Services;

public sealed class CacheService(LocalDb localDb) : ICacheService
{
    public async Task<List<CachedICS>> GetCachedICSAsync(Guid employeeId)
    {
        var db = await localDb.GetConnectionAsync();
        var employeeIdStr = employeeId.ToString();
        return await db.Table<CachedICS>()
            .Where(x => x.EmployeeId == employeeIdStr)
            .ToListAsync();
    }

    public async Task UpsertICSAsync(IEnumerable<CachedICS> items)
    {
        var db = await localDb.GetConnectionAsync();
        foreach (var item in items)
            await db.InsertOrReplaceAsync(item);
    }

    public async Task<List<CachedPAR>> GetCachedPARAsync(Guid employeeId)
    {
        var db = await localDb.GetConnectionAsync();
        var employeeIdStr = employeeId.ToString();
        return await db.Table<CachedPAR>()
            .Where(x => x.EmployeeId == employeeIdStr)
            .ToListAsync();
    }

    public async Task UpsertPARAsync(IEnumerable<CachedPAR> items)
    {
        var db = await localDb.GetConnectionAsync();
        foreach (var item in items)
            await db.InsertOrReplaceAsync(item);
    }

    public async Task SaveEmployeeProfileAsync(CachedEmployeeProfile profile)
    {
        var db = await localDb.GetConnectionAsync();
        await db.InsertOrReplaceAsync(profile);
    }

    public async Task<CachedEmployeeProfile?> GetEmployeeProfileAsync(string userId)
    {
        var db = await localDb.GetConnectionAsync();
        return await db.Table<CachedEmployeeProfile>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveUserIdentityAsync(CachedUserIdentity identity)
    {
        var db = await localDb.GetConnectionAsync();
        await db.InsertOrReplaceAsync(identity);
    }

    public async Task<CachedUserIdentity?> GetUserIdentityAsync(string userId)
    {
        var db = await localDb.GetConnectionAsync();
        return await db.Table<CachedUserIdentity>()
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task ClearAllAsync()
    {
        var db = await localDb.GetConnectionAsync();
        await db.DeleteAllAsync<CachedUserIdentity>();
        await db.DeleteAllAsync<CachedEmployeeProfile>();
        await db.DeleteAllAsync<CachedICS>();
        await db.DeleteAllAsync<CachedPAR>();
    }
}
