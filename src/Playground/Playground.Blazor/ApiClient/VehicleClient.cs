using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;

namespace FSH.Playground.Blazor.ApiClient;

internal interface IVehicleClient
{
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default);
    Task<PagedResponse<VehicleDto>> SearchVehiclesAsync(SearchVehiclesQuery query, CancellationToken cancellationToken = default);
    Task<VehicleDto?> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VehicleDto> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken = default);
    Task AssignVehicleAsync(Guid id, AssignVehicleCommand command, CancellationToken cancellationToken = default);
    Task UpdateOdometerAsync(Guid id, UpdateOdometerCommand command, CancellationToken cancellationToken = default);
    Task RetireVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task DecommissionVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReactivateVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteVehicleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RepairRecordDto> CreateRepairAsync(CreateRepairRecordCommand command, CancellationToken cancellationToken = default);
    Task<PagedResponse<RepairRecordDto>> SearchRepairsAsync(SearchRepairRecordsQuery query, CancellationToken cancellationToken = default);
    Task<RepairRecordDto?> GetRepairAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepairRecordDto> UpdateRepairAsync(Guid id, UpdateRepairRecordCommand command, CancellationToken cancellationToken = default);
    Task StartRepairAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompleteRepairAsync(Guid id, CompleteRepairCommand command, CancellationToken cancellationToken = default);
    Task CancelRepairAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteRepairAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MaintenanceScheduleDto> CreateMaintenanceScheduleAsync(CreateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default);
    Task<List<MaintenanceScheduleDto>> SearchMaintenanceSchedulesAsync(MaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default);
    Task<List<MaintenanceScheduleDto>> GetDueMaintenanceSchedulesAsync(DueMaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceScheduleDto?> GetMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<MaintenanceScheduleDto> UpdateMaintenanceScheduleAsync(Guid scheduleId, UpdateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default);
    Task DeactivateMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task DeleteMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<MaintenanceLogDto> LogMaintenanceCompletionAsync(LogMaintenanceCompletionRequest request, CancellationToken cancellationToken = default);
    Task<List<MaintenanceLogDto>> SearchMaintenanceLogsAsync(MaintenanceLogSearchRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceLogDto?> GetMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default);
    Task<MaintenanceLogDto> UpdateMaintenanceLogAsync(Guid logId, UpdateMaintenanceLogRequest request, CancellationToken cancellationToken = default);
    Task DeleteMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default);

    Task<List<MotorVehicleInventoryItemDto>> GetMotorVehicleInventoryAsync(
        string? status = null, CancellationToken cancellationToken = default);
}

internal sealed class VehicleClient : IVehicleClient
{
    private readonly HttpClient _httpClient;

    public VehicleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<VehicleDto> CreateVehicleAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<VehicleDto>("api/v1/vehicle/vehicles", command, cancellationToken);

    public async Task<PagedResponse<VehicleDto>> SearchVehiclesAsync(SearchVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl("api/v1/vehicle/vehicles", new Dictionary<string, string?>
        {
            ["keyword"] = query.Keyword,
            ["status"] = query.Status,
            ["type"] = query.Type,
            ["assignedDepartmentId"] = query.AssignedDepartmentId?.ToString(),
            ["pageNumber"] = query.PageNumber?.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = query.PageSize?.ToString(CultureInfo.InvariantCulture),
            ["sort"] = query.Sort
        });

        var response = await _httpClient.GetFromJsonAsync<PagedResponse<VehicleDto>>(url, cancellationToken);
        return response ?? new PagedResponse<VehicleDto> { Items = [] };
    }

    public Task<VehicleDto?> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<VehicleDto>($"api/v1/vehicle/vehicles/{id}", cancellationToken);

    public Task<VehicleDto> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken = default) =>
        PutJsonAsync<VehicleDto>($"api/v1/vehicle/vehicles/{id}", command, cancellationToken);

    public Task AssignVehicleAsync(Guid id, AssignVehicleCommand command, CancellationToken cancellationToken = default) =>
        PutNoContentAsync($"api/v1/vehicle/vehicles/{id}/assignment", command, cancellationToken);

    public Task UpdateOdometerAsync(Guid id, UpdateOdometerCommand command, CancellationToken cancellationToken = default) =>
        PutNoContentAsync($"api/v1/vehicle/vehicles/{id}/odometer", command, cancellationToken);

    public Task RetireVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync($"api/v1/vehicle/vehicles/{id}/retire", cancellationToken);

    public Task DecommissionVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync($"api/v1/vehicle/vehicles/{id}/decommission", cancellationToken);

    public Task ReactivateVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync($"api/v1/vehicle/vehicles/{id}/reactivate", cancellationToken);

    public async Task DeleteVehicleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri($"api/v1/vehicle/vehicles/{id}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<RepairRecordDto> CreateRepairAsync(CreateRepairRecordCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<RepairRecordDto>("api/v1/vehicle/repairs", command, cancellationToken);

    public async Task<PagedResponse<RepairRecordDto>> SearchRepairsAsync(SearchRepairRecordsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl("api/v1/vehicle/repairs", new Dictionary<string, string?>
        {
            ["vehicleId"] = query.VehicleId?.ToString(),
            ["status"] = query.Status,
            ["dateFrom"] = query.DateFrom?.ToString("O", CultureInfo.InvariantCulture),
            ["dateTo"] = query.DateTo?.ToString("O", CultureInfo.InvariantCulture),
            ["keyword"] = query.Keyword,
            ["pageNumber"] = query.PageNumber?.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = query.PageSize?.ToString(CultureInfo.InvariantCulture),
            ["sort"] = query.Sort
        });

        var response = await _httpClient.GetFromJsonAsync<PagedResponse<RepairRecordDto>>(url, cancellationToken);
        return response ?? new PagedResponse<RepairRecordDto> { Items = [] };
    }

    public Task<RepairRecordDto?> GetRepairAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<RepairRecordDto>($"api/v1/vehicle/repairs/{id}", cancellationToken);

    public Task<RepairRecordDto> UpdateRepairAsync(Guid id, UpdateRepairRecordCommand command, CancellationToken cancellationToken = default) =>
        PutJsonAsync<RepairRecordDto>($"api/v1/vehicle/repairs/{id}", command, cancellationToken);

    public Task StartRepairAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync($"api/v1/vehicle/repairs/{id}/start", cancellationToken);

    public Task CompleteRepairAsync(Guid id, CompleteRepairCommand command, CancellationToken cancellationToken = default) =>
        PostNoContentAsync($"api/v1/vehicle/repairs/{id}/complete", command, cancellationToken);

    public Task CancelRepairAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync($"api/v1/vehicle/repairs/{id}/cancel", cancellationToken);

    public async Task DeleteRepairAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri($"api/v1/vehicle/repairs/{id}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<MaintenanceScheduleDto> CreateMaintenanceScheduleAsync(CreateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync<MaintenanceScheduleDto>("api/v1/vehicle/maintenance/schedules", request, cancellationToken);

    public async Task<List<MaintenanceScheduleDto>> SearchMaintenanceSchedulesAsync(MaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceScheduleDto>>("api/v1/vehicle/maintenance/schedules/search", request, cancellationToken);
        return response ?? [];
    }

    public async Task<List<MaintenanceScheduleDto>> GetDueMaintenanceSchedulesAsync(DueMaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceScheduleDto>>("api/v1/vehicle/maintenance/schedules/due", request, cancellationToken);
        return response ?? [];
    }

    public Task<MaintenanceScheduleDto?> GetMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<MaintenanceScheduleDto>($"api/v1/vehicle/maintenance/schedules/{scheduleId}", cancellationToken);

    public Task<MaintenanceScheduleDto> UpdateMaintenanceScheduleAsync(Guid scheduleId, UpdateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync<MaintenanceScheduleDto>($"api/v1/vehicle/maintenance/schedules/{scheduleId}", request, cancellationToken);

    public Task DeactivateMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        PatchNoBodyNoContentAsync($"api/v1/vehicle/maintenance/schedules/{scheduleId}/deactivate", cancellationToken);

    public async Task DeleteMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri($"api/v1/vehicle/maintenance/schedules/{scheduleId}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<MaintenanceLogDto> LogMaintenanceCompletionAsync(LogMaintenanceCompletionRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync<MaintenanceLogDto>("api/v1/vehicle/maintenance/logs", request, cancellationToken);

    public async Task<List<MaintenanceLogDto>> SearchMaintenanceLogsAsync(MaintenanceLogSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceLogDto>>("api/v1/vehicle/maintenance/logs/search", request, cancellationToken);
        return response ?? [];
    }

    public Task<MaintenanceLogDto?> GetMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<MaintenanceLogDto>($"api/v1/vehicle/maintenance/logs/{logId}", cancellationToken);

    public Task<MaintenanceLogDto> UpdateMaintenanceLogAsync(Guid logId, UpdateMaintenanceLogRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync<MaintenanceLogDto>($"api/v1/vehicle/maintenance/logs/{logId}", request, cancellationToken);

    public async Task DeleteMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri($"api/v1/vehicle/maintenance/logs/{logId}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<MotorVehicleInventoryItemDto>> GetMotorVehicleInventoryAsync(
        string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/vehicle/vehicles/inventory-report", new Dictionary<string, string?>
        {
            ["status"] = status
        });
        var result = await _httpClient.GetFromJsonAsync<List<MotorVehicleInventoryItemDto>>(url, cancellationToken);
        return result ?? [];
    }

    private async Task<T> PostJsonAsync<T>(string url, object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException($"Empty response for POST {url}.");
    }

    private async Task<T> PutJsonAsync<T>(string url, object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException($"Empty response for PUT {url}.");
    }

    private async Task<T?> GetJsonOrNullAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(new Uri(url, UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task PutNoContentAsync(string url, object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task PostNoContentAsync(string url, object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task PostNoBodyNoContentAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(new Uri(url, UriKind.Relative), content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task PatchNoBodyNoContentAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string BuildUrl(string basePath, Dictionary<string, string?> queryParams)
    {
        var query = new StringBuilder();
        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            query.Append(query.Length == 0 ? '?' : '&');
            query.Append(Uri.EscapeDataString(key));
            query.Append('=');
            query.Append(Uri.EscapeDataString(value));
        }

        return basePath + query;
    }
}