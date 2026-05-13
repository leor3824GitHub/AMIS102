using System.Net.Http;
using System.Net.Http.Json;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;

namespace AMIS.Playground.Blazor.ApiClient;

internal interface IReportSignatoryClient
{
    Task<List<ReportSignatoryDto>> GetReportSignatoriesAsync(string reportType, CancellationToken cancellationToken = default);
    Task<ReportSignatoryDto> CreateAsync(CreateReportSignatoryCommand command, CancellationToken cancellationToken = default);
    Task<ReportSignatoryDto> UpdateAsync(Guid id, UpdateReportSignatoryCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class ReportSignatoryClient(HttpClient httpClient) : IReportSignatoryClient
{
    public async Task<List<ReportSignatoryDto>> GetReportSignatoriesAsync(
        string reportType, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/master-data/report-signatories?reportType={Uri.EscapeDataString(reportType)}";
        var result = await httpClient.GetFromJsonAsync<List<ReportSignatoryDto>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<ReportSignatoryDto> CreateAsync(
        CreateReportSignatoryCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/master-data/report-signatories", command, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReportSignatoryDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response.");
    }

    public async Task<ReportSignatoryDto> UpdateAsync(
        Guid id, UpdateReportSignatoryCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/v1/master-data/report-signatories/{id}", command, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReportSignatoryDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            new Uri($"api/v1/master-data/report-signatories/{id}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

