using System.Net.Http.Json;

namespace Playground.Maui.Services;

public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    public async Task<TokenResponse> IssueTokenAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/v1/identity/token/issue",
            new TokenIssueRequest(email, password),
            ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>(ct))!;
    }

    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<UserProfileDto>("api/v1/identity/profile", ct);
        return result!;
    }

    public async Task<MyEmployeeDto> GetMyEmployeeAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<MyEmployeeDto>("api/v1/master-data/employees/me", ct);
        return result!;
    }

    public async Task<List<ICSSummaryDto>> GetMyICSListAsync(Guid employeeId, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PagedResult<ICSSummaryDto>>(
            $"api/v1/asset-management/inventory-custodian-slips?ReceivedByEmployeeId={employeeId}&PageSize=200",
            ct);
        return result?.Data ?? [];
    }

    public async Task<ICSDetailDto> GetICSByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ICSDetailDto>(
            $"api/v1/asset-management/inventory-custodian-slips/{id}", ct);
        return result!;
    }

    public async Task<List<PARSummaryDto>> GetMyPARListAsync(Guid employeeId, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PagedResult<PARSummaryDto>>(
            $"api/v1/asset-management/property-acknowledgement-receipts?ReceivedByEmployeeId={employeeId}&PageSize=200",
            ct);
        return result?.Data ?? [];
    }

    public async Task<PARDetailDto> GetPARByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PARDetailDto>(
            $"api/v1/asset-management/property-acknowledgement-receipts/{id}", ct);
        return result!;
    }

    public async Task<TangibleInventoryItemDetailDto> GetItemByPropertyNoAsync(string propertyNo, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<TangibleInventoryItemDetailDto>(
            $"api/v1/asset-management/tangible-inventory-items/by-property-no/{Uri.EscapeDataString(propertyNo)}",
            ct);
        return result!;
    }

    private sealed record PagedResult<T>(List<T> Data, int TotalCount);
}
