using Playground.Maui.Data.Models;

namespace Playground.Maui.Services;

public interface ICacheService
{
    Task<List<CachedICS>> GetCachedICSAsync(Guid employeeId);
    Task UpsertICSAsync(IEnumerable<CachedICS> items);
    Task<List<CachedPAR>> GetCachedPARAsync(Guid employeeId);
    Task UpsertPARAsync(IEnumerable<CachedPAR> items);
    Task SaveEmployeeProfileAsync(CachedEmployeeProfile profile);
    Task<CachedEmployeeProfile?> GetEmployeeProfileAsync(string userId);
    Task ClearAllAsync();
}
