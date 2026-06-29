using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Blazor.ApiClient;

internal interface IVehicleClient
{
    Task<VehicleDto> EnrollVehicleAsync(EnrollVehicleCommand command, CancellationToken cancellationToken = default);
    Task<List<EnrollableVehicleAssetDto>> GetEnrollableVehicleAssetsAsync(string? keyword = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<VehicleDto>> SearchVehiclesAsync(SearchVehiclesQuery query, CancellationToken cancellationToken = default);
    Task<VehicleDto?> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VehicleDto> UpdateVehicleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken = default);
    Task UpdateOdometerAsync(Guid id, UpdateOdometerCommand command, CancellationToken cancellationToken = default);
    Task RetireVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task DecommissionVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReactivateVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteVehicleAsync(Guid id, CancellationToken cancellationToken = default);

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
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateVehicleInventoryFastPdfAsync(
        string? status = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        CancellationToken cancellationToken = default);

    Task<List<VehicleDto>> GetMyVehiclesAsync(CancellationToken cancellationToken = default);
    Task<VehicleDailyUsageDto> RecordMyVehicleFuelOdometerAsync(RecordMyVehicleFuelOdometerCommand command, CancellationToken cancellationToken = default);
    Task<VehicleDailyUsageSummaryDto> GetMyVehicleDailyUsageAsync(GetMyVehicleDailyUsageQuery query, CancellationToken cancellationToken = default);

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
        public const string MaintenanceSchedules = Root + "/maintenance/schedules";
        public const string MaintenanceSchedulesSearch = MaintenanceSchedules + "/search";
        public const string MaintenanceSchedulesDue = MaintenanceSchedules + "/due";
        public const string MaintenanceLogs = Root + "/maintenance/logs";
        public const string MaintenanceLogsSearch = MaintenanceLogs + "/search";
        public const string EnrollableAssets = Vehicles + "/enrollable-assets";
        public const string InventoryReport = Vehicles + "/inventory-report";
        public const string InventoryPdf = Vehicles + "/inventory/pdf";
        public const string InventoryFastPdf = "api/v1/fast-reporting/vehicle/inventory/print";
        public const string FuelOdometer = Root + "/fuel-odometer";
        public const string FuelOdometerSummary = FuelOdometer + "/summary";
        public const string MyVehicles = Root + "/my-vehicles";
        public const string MyVehiclesFuelOdometer = MyVehicles + "/fuel-odometer";
        public const string MyVehiclesFuelOdometerSummary = MyVehiclesFuelOdometer + "/summary";

        public static string VehicleById(Guid id) => $"{Vehicles}/{id}";
        public static string VehicleOdometer(Guid id) => $"{Vehicles}/{id}/odometer";
        public static string VehicleRetire(Guid id) => $"{Vehicles}/{id}/retire";
        public static string VehicleDecommission(Guid id) => $"{Vehicles}/{id}/decommission";
        public static string VehicleReactivate(Guid id) => $"{Vehicles}/{id}/reactivate";

        public static string MaintenanceScheduleById(Guid id) => $"{MaintenanceSchedules}/{id}";
        public static string MaintenanceScheduleDeactivate(Guid id) => $"{MaintenanceSchedules}/{id}/deactivate";

        public static string MaintenanceLogById(Guid id) => $"{MaintenanceLogs}/{id}";
        public static string FuelOdometerById(Guid id) => $"{FuelOdometer}/{id}";
    }

    public VehicleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<VehicleDto> EnrollVehicleAsync(EnrollVehicleCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<VehicleDto>(VehicleApiRoutes.Vehicles, command, cancellationToken);

    public async Task<List<EnrollableVehicleAssetDto>> GetEnrollableVehicleAssetsAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(VehicleApiRoutes.EnrollableAssets, new Dictionary<string, string?>
        {
            ["keyword"] = keyword
        });
        var result = await _httpClient.GetFromJsonAsync<List<EnrollableVehicleAssetDto>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<PagedResponse<VehicleDto>> SearchVehiclesAsync(SearchVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.Vehicles, new Dictionary<string, string?>
        {
            ["keyword"] = query.Keyword,
            ["status"] = query.Status,
            ["type"] = query.Type,
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
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/quest-pdf-reporting/vehicle/inventory/pdf", new Dictionary<string, string?>
        {
            ["status"] = status,
            ["asOfDate"] = asOfDate?.ToString("O"),
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["marginMm"] = marginMm?.ToString(CultureInfo.InvariantCulture),
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> GenerateVehicleInventoryFastPdfAsync(
        string? status = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(VehicleApiRoutes.InventoryFastPdf, new Dictionary<string, string?>
        {
            ["status"] = status,
            ["asOfDate"] = asOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["pageWidth"] = pageWidth
        });
        return await _httpClient.GetByteArrayAsync(new Uri(url, UriKind.Relative), cancellationToken);
    }

    public async Task<List<VehicleDto>> GetMyVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<VehicleDto>>(VehicleApiRoutes.MyVehicles, cancellationToken);
        return result ?? [];
    }

    public Task<VehicleDailyUsageDto> RecordMyVehicleFuelOdometerAsync(RecordMyVehicleFuelOdometerCommand command, CancellationToken cancellationToken = default) =>
        PostJsonAsync<VehicleDailyUsageDto>(VehicleApiRoutes.MyVehiclesFuelOdometer, command, cancellationToken);

    public async Task<VehicleDailyUsageSummaryDto> GetMyVehicleDailyUsageAsync(GetMyVehicleDailyUsageQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var url = BuildUrl(VehicleApiRoutes.MyVehiclesFuelOdometerSummary, new Dictionary<string, string?>
        {
            ["vehicleId"] = query.VehicleId.ToString(),
            ["dateFrom"] = query.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = query.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });

        var response = await _httpClient.GetFromJsonAsync<VehicleDailyUsageSummaryDto>(url, cancellationToken);
        return response ?? new VehicleDailyUsageSummaryDto(0, 0, 0m, 0m, 0m, 0m, 0m);
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
