using System.Globalization;
using System.Net.Http.Json;
using System.Text;

namespace AMIS.Playground.Blazor.Services.AssetRegister;

public interface IAssetRegisterReportsClient
{
    Task<RegSpiReportDto?> GetRegSpiReportAsync(DateOnly asOfDate, Guid? custodianId = null, CancellationToken cancellationToken = default);
    Task<PhysicalCountReportDto?> GetPhysicalCountReportAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IssuanceReportDocumentDto?> GetIssuanceReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<AccountabilityReportDto?> GetAccountabilityReportAsync(Guid accountabilityId, CancellationToken cancellationToken = default);
    Task<IncidentReportDocumentDto?> GetIncidentReportAsync(Guid incidentReportId, CancellationToken cancellationToken = default);
    Task<UnserviceableReportDocumentDto?> GetUnserviceableReportAsync(Guid reportId, CancellationToken cancellationToken = default);
}

public sealed class AssetRegisterReportsClient(HttpClient httpClient) : IAssetRegisterReportsClient
{
    public Task<RegSpiReportDto?> GetRegSpiReportAsync(
        DateOnly asOfDate,
        Guid? custodianId = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/regspi", new Dictionary<string, string?>
        {
            ["asOfDate"] = asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString()
        });

        return httpClient.GetFromJsonAsync<RegSpiReportDto>(url, cancellationToken);
    }

    public Task<PhysicalCountReportDto?> GetPhysicalCountReportAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<PhysicalCountReportDto>($"api/v1/asset-register/reports/count/{sessionId}", cancellationToken);

    public Task<IssuanceReportDocumentDto?> GetIssuanceReportAsync(Guid reportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<IssuanceReportDocumentDto>($"api/v1/asset-register/reports/issuance/{reportId}", cancellationToken);

    public Task<AccountabilityReportDto?> GetAccountabilityReportAsync(Guid accountabilityId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<AccountabilityReportDto>($"api/v1/asset-register/reports/accountability/{accountabilityId}", cancellationToken);

    public Task<IncidentReportDocumentDto?> GetIncidentReportAsync(Guid incidentReportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<IncidentReportDocumentDto>($"api/v1/asset-register/reports/incidents/{incidentReportId}", cancellationToken);

    public Task<UnserviceableReportDocumentDto?> GetUnserviceableReportAsync(Guid reportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<UnserviceableReportDocumentDto>($"api/v1/asset-register/reports/unserviceable/{reportId}", cancellationToken);

    private static string BuildUrl(string path, Dictionary<string, string?> query)
    {
        var builder = new StringBuilder(path);
        var hasQuery = false;

        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append(hasQuery ? '&' : '?');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            hasQuery = true;
        }

        return builder.ToString();
    }
}

