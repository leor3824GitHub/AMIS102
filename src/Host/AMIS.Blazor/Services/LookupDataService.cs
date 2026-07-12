using System.Net.Http.Json;
using AMIS.Blazor.ApiClient;

namespace AMIS.Blazor.Services;

internal enum LookupSet
{
    Departments,
    Offices,
    Positions,
    UnitOfMeasures,
    Categories,
    Suppliers
}

/// <summary>
/// Per-circuit cache for bounded reference-data lists (departments, offices, positions,
/// unit-of-measures, categories). Loads each set once via the unpaged "all active" lookup
/// endpoints — which never truncate at the 100-row page-size clamp — then serves from memory
/// until the TTL lapses or an admin CRUD page calls <see cref="Invalidate"/>. Follows the
/// idempotent load-once pattern of <see cref="ICapitalizationThresholdState"/>.
/// </summary>
internal interface ILookupDataService
{
    Task<IReadOnlyList<DepartmentReferenceDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OfficeReferenceDto>> GetOfficesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PositionReferenceDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitOfMeasureReferenceDto>> GetUnitOfMeasuresAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolves many employee references in one call — replaces per-row employee fetch loops.</summary>
    Task<IReadOnlyDictionary<Guid, EmployeeReferenceDto>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default);

    void Invalidate(LookupSet lookupSet);
}

internal sealed class LookupDataService(HttpClient httpClient) : ILookupDataService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly Dictionary<LookupSet, (object Task, DateTimeOffset LoadedAt)> _cache = new();
    private readonly object _gate = new();

    public Task<IReadOnlyList<DepartmentReferenceDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<DepartmentReferenceDto>(LookupSet.Departments, "departments", cancellationToken);

    public Task<IReadOnlyList<OfficeReferenceDto>> GetOfficesAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<OfficeReferenceDto>(LookupSet.Offices, "offices", cancellationToken);

    public Task<IReadOnlyList<PositionReferenceDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<PositionReferenceDto>(LookupSet.Positions, "positions", cancellationToken);

    public Task<IReadOnlyList<UnitOfMeasureReferenceDto>> GetUnitOfMeasuresAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<UnitOfMeasureReferenceDto>(LookupSet.UnitOfMeasures, "unit-of-measures", cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<CategoryDto>(LookupSet.Categories, "categories", cancellationToken);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => GetOrLoadAsync<SupplierDto>(LookupSet.Suppliers, "suppliers", cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, EmployeeReferenceDto>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        if (employeeIds.Count == 0)
            return new Dictionary<Guid, EmployeeReferenceDto>();

        try
        {
            var response = await httpClient
                .PostAsJsonAsync("api/v1/master-data/lookup/employees/by-ids", employeeIds, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<Dictionary<Guid, EmployeeReferenceDto>>(cancellationToken)
                .ConfigureAwait(false);

            return result ?? new Dictionary<Guid, EmployeeReferenceDto>();
        }
        catch
        {
            return new Dictionary<Guid, EmployeeReferenceDto>();
        }
    }

    public void Invalidate(LookupSet lookupSet)
    {
        lock (_gate)
        {
            _cache.Remove(lookupSet);
        }
    }

    private Task<IReadOnlyList<T>> GetOrLoadAsync<T>(LookupSet lookupSet, string path, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(lookupSet, out var cached) &&
                cached.Task is Task<IReadOnlyList<T>> existing &&
                !(existing.IsCompletedSuccessfully && DateTimeOffset.UtcNow - cached.LoadedAt > Ttl))
            {
                return existing;
            }

            var task = LoadAsync<T>(lookupSet, path, cancellationToken);
            _cache[lookupSet] = (task, DateTimeOffset.UtcNow);
            return task;
        }
    }

    private async Task<IReadOnlyList<T>> LoadAsync<T>(LookupSet lookupSet, string path, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"api/v1/master-data/lookup/{path}/all";
            var result = await httpClient.GetFromJsonAsync<List<T>>(url, cancellationToken).ConfigureAwait(false);
            return result ?? [];
        }
        catch
        {
            // Drop the cached (failed) entry so the next access retries instead of caching an empty list.
            Invalidate(lookupSet);
            return [];
        }
    }
}
