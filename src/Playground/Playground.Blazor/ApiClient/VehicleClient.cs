using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Playground.Blazor.ApiClient;

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

    Task<byte[]> GenerateVehicleInventoryPdfAsync(
        string? status = null,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<VehicleDailyUsageDto> CreateVehicleDailyUsageAsync(CreateVehicleDailyUsageCommand command, CancellationToken cancellationToken = default);
    Task<VehicleDailyUsageDto> UpdateVehicleDailyUsageAsync(Guid id, UpdateVehicleDailyUsageCommand command, CancellationToken cancellationToken = default);
    Task<PagedResponse<VehicleDailyUsageDto>> SearchVehicleDailyUsageAsync(SearchVehicleDailyUsageQuery query, CancellationToken cancellationToken = default);
    Task<VehicleDailyUsageSummaryDto> GetVehicleDailyUsageSummaryAsync(GetVehicleDailyUsageSummaryQuery query, CancellationToken cancellationToken = default);
}

internal sealed class VehicleClient : IVehicleClient
{
    private readonly HttpClient _httpClient;

    private static class VehicleApiRoutes
    {
        private const string Root = "api/v1/vehicle";

        public const string Vehicles = Root + "/vehicles";
        public const string Repairs = Root + "/repairs";
        public const string MaintenanceSchedules = Root + "/maintenance/schedules";
        public const string MaintenanceSchedulesSearch = MaintenanceSchedules + "/search";
        public const string MaintenanceSchedulesDue = MaintenanceSchedules + "/due";
        public const string MaintenanceLogs = Root + "/maintenance/logs";
        public const string MaintenanceLogsSearch = MaintenanceLogs + "/search";
        public const string InventoryReport = Vehicles + "/inventory-report";
        public const string InventoryPdf = Vehicles + "/inventory/pdf";
        public const string FuelOdometer = Root + "/fuel-odometer";
        public const string FuelOdometerSummary = FuelOdometer + "/summary";

        public static string VehicleById(Guid id) => $"{Vehicles}/{id}";
        public static string VehicleAssignment(Guid id) => $"{Vehicles}/{id}/assignment";
        public static string VehicleOdometer(Guid id) => $"{Vehicles}/{id}/odometer";
        public static string VehicleRetire(Guid id) => $"{Vehicles}/{id}/retire";
        public static string VehicleDecommission(Guid id) => $"{Vehicles}/{id}/decommission";
        public static string VehicleReactivate(Guid id) => $"{Vehicles}/{id}/reactivate";

        public static string RepairById(Guid id) => $"{Repairs}/{id}";
        public static string RepairStart(Guid id) => $"{Repairs}/{id}/start";
        public static string RepairComplete(Guid id) => $"{Repairs}/{id}/complete";
        public static string RepairCancel(Guid id) => $"{Repairs}/{id}/cancel";

        public static string MaintenanceScheduleById(Guid id) => $"{MaintenanceSchedules}/{id}";
        public static string MaintenanceScheduleDeactivate(Guid id) => $"{MaintenanceSchedules}/{id}/deactivate";

        public static string MaintenanceLogById(Guid id) => $"{MaintenanceLogs}/{id}";
        public static string FuelOdometerById(Guid id) => $"{FuelOdometer}/{id}";
    }

    public VehicleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<VehicleDto> CreateVehicleAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<VehicleDto>(VehicleApiRoutes.Vehicles, command, cancellationToken);

    public async Task<PagedResponse<VehicleDto>> SearchVehiclesAsync(SearchVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.Vehicles, new Dictionary<string, string?>
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
        GetJsonOrNullAsync<VehicleDto>(VehicleApiRoutes.VehicleById(id), cancellationToken);

    public Task<VehicleDto> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken = default) =>
        PutJsonAsync<VehicleDto>(VehicleApiRoutes.VehicleById(id), command, cancellationToken);

    public Task AssignVehicleAsync(Guid id, AssignVehicleCommand command, CancellationToken cancellationToken = default) =>
        PutNoContentAsync(VehicleApiRoutes.VehicleAssignment(id), command, cancellationToken);

    public Task UpdateOdometerAsync(Guid id, UpdateOdometerCommand command, CancellationToken cancellationToken = default) =>
        PutNoContentAsync(VehicleApiRoutes.VehicleOdometer(id), command, cancellationToken);

    public Task RetireVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync(VehicleApiRoutes.VehicleRetire(id), cancellationToken);

    public Task DecommissionVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync(VehicleApiRoutes.VehicleDecommission(id), cancellationToken);

    public Task ReactivateVehicleAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync(VehicleApiRoutes.VehicleReactivate(id), cancellationToken);

    public async Task DeleteVehicleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri(VehicleApiRoutes.VehicleById(id), UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<RepairRecordDto> CreateRepairAsync(CreateRepairRecordCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<RepairRecordDto>(VehicleApiRoutes.Repairs, command, cancellationToken);

    public async Task<PagedResponse<RepairRecordDto>> SearchRepairsAsync(SearchRepairRecordsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.Repairs, new Dictionary<string, string?>
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
        GetJsonOrNullAsync<RepairRecordDto>(VehicleApiRoutes.RepairById(id), cancellationToken);

    public Task<RepairRecordDto> UpdateRepairAsync(Guid id, UpdateRepairRecordCommand command, CancellationToken cancellationToken = default) =>
        PutJsonAsync<RepairRecordDto>(VehicleApiRoutes.RepairById(id), command, cancellationToken);

    public Task StartRepairAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync(VehicleApiRoutes.RepairStart(id), cancellationToken);

    public Task CompleteRepairAsync(Guid id, CompleteRepairCommand command, CancellationToken cancellationToken = default) =>
        PostNoContentAsync(VehicleApiRoutes.RepairComplete(id), command, cancellationToken);

    public Task CancelRepairAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostNoBodyNoContentAsync(VehicleApiRoutes.RepairCancel(id), cancellationToken);

    public async Task DeleteRepairAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri(VehicleApiRoutes.RepairById(id), UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<MaintenanceScheduleDto> CreateMaintenanceScheduleAsync(CreateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync<MaintenanceScheduleDto>(VehicleApiRoutes.MaintenanceSchedules, request, cancellationToken);

    public async Task<List<MaintenanceScheduleDto>> SearchMaintenanceSchedulesAsync(MaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceScheduleDto>>(VehicleApiRoutes.MaintenanceSchedulesSearch, request, cancellationToken);
        return response ?? [];
    }

    public async Task<List<MaintenanceScheduleDto>> GetDueMaintenanceSchedulesAsync(DueMaintenanceScheduleSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceScheduleDto>>(VehicleApiRoutes.MaintenanceSchedulesDue, request, cancellationToken);
        return response ?? [];
    }

    public Task<MaintenanceScheduleDto?> GetMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<MaintenanceScheduleDto>(VehicleApiRoutes.MaintenanceScheduleById(scheduleId), cancellationToken);

    public Task<MaintenanceScheduleDto> UpdateMaintenanceScheduleAsync(Guid scheduleId, UpdateMaintenanceScheduleRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync<MaintenanceScheduleDto>(VehicleApiRoutes.MaintenanceScheduleById(scheduleId), request, cancellationToken);

    public Task DeactivateMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        PatchNoBodyNoContentAsync(VehicleApiRoutes.MaintenanceScheduleDeactivate(scheduleId), cancellationToken);

    public async Task DeleteMaintenanceScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri(VehicleApiRoutes.MaintenanceScheduleById(scheduleId), UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<MaintenanceLogDto> LogMaintenanceCompletionAsync(LogMaintenanceCompletionRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync<MaintenanceLogDto>(VehicleApiRoutes.MaintenanceLogs, request, cancellationToken);

    public async Task<List<MaintenanceLogDto>> SearchMaintenanceLogsAsync(MaintenanceLogSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync<List<MaintenanceLogDto>>(VehicleApiRoutes.MaintenanceLogsSearch, request, cancellationToken);
        return response ?? [];
    }

    public Task<MaintenanceLogDto?> GetMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default) =>
        GetJsonOrNullAsync<MaintenanceLogDto>(VehicleApiRoutes.MaintenanceLogById(logId), cancellationToken);

    public Task<MaintenanceLogDto> UpdateMaintenanceLogAsync(Guid logId, UpdateMaintenanceLogRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync<MaintenanceLogDto>(VehicleApiRoutes.MaintenanceLogById(logId), request, cancellationToken);

    public async Task DeleteMaintenanceLogAsync(Guid logId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(new Uri(VehicleApiRoutes.MaintenanceLogById(logId), UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<MotorVehicleInventoryItemDto>> GetMotorVehicleInventoryAsync(
        string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(VehicleApiRoutes.InventoryReport, new Dictionary<string, string?>
        {
            ["status"] = status
        });
        var result = await _httpClient.GetFromJsonAsync<List<MotorVehicleInventoryItemDto>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<byte[]> GenerateVehicleInventoryPdfAsync(
        string? status = null,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var command = new { Status = status, AsOfDate = asOfDate };
        using var response = await _httpClient.PostAsJsonAsync(
            VehicleApiRoutes.InventoryPdf, command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public Task<VehicleDailyUsageDto> CreateVehicleDailyUsageAsync(CreateVehicleDailyUsageCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<VehicleDailyUsageDto>(VehicleApiRoutes.FuelOdometer, command, cancellationToken);

    public Task<VehicleDailyUsageDto> UpdateVehicleDailyUsageAsync(Guid id, UpdateVehicleDailyUsageCommand command, CancellationToken cancellationToken = default) =>
        PutJsonAsync<VehicleDailyUsageDto>(VehicleApiRoutes.FuelOdometerById(id), command, cancellationToken);

    public async Task<PagedResponse<VehicleDailyUsageDto>> SearchVehicleDailyUsageAsync(SearchVehicleDailyUsageQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.FuelOdometer, new Dictionary<string, string?>
        {
            ["vehicleId"] = query.VehicleId?.ToString(),
            ["dateFrom"] = query.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = query.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["pageNumber"] = query.PageNumber?.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = query.PageSize?.ToString(CultureInfo.InvariantCulture),
            ["sort"] = query.Sort
        });

        var response = await _httpClient.GetFromJsonAsync<PagedResponse<VehicleDailyUsageDto>>(url, cancellationToken);
        return response ?? new PagedResponse<VehicleDailyUsageDto> { Items = [] };
    }

    public async Task<VehicleDailyUsageSummaryDto> GetVehicleDailyUsageSummaryAsync(GetVehicleDailyUsageSummaryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.FuelOdometerSummary, new Dictionary<string, string?>
        {
            ["vehicleId"] = query.VehicleId?.ToString(),
            ["dateFrom"] = query.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = query.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });

        var response = await _httpClient.GetFromJsonAsync<VehicleDailyUsageSummaryDto>(url, cancellationToken);
        return response ?? new VehicleDailyUsageSummaryDto(0, 0, 0m, 0m, 0m, 0m, 0m);
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
