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

    // ── PDF (QuestPDF) ──────────────────────────────────────────────────────
    Task<byte[]> GetPhysicalCountPdfAsync(Guid sessionId, bool ppe, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetRegSpiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetAccountabilityPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetUnserviceablePdfAsync(Guid reportId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetIncidentPdfAsync(Guid incidentReportId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetPropertyCardPdfAsync(string propertyNo, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetIcsStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetParStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
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

    private const string PdfBase = "api/v1/quest-pdf-reporting/asset-register";

    public Task<byte[]> GetPhysicalCountPdfAsync(Guid sessionId, bool ppe, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var doc = ppe ? "rpcppe" : "rpcsemex";
        var url = BuildUrl($"{PdfBase}/physical-count/{sessionId}/{doc}/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetRegSpiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/regspi/pdf", new()
        {
            ["asOfDate"] = asOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString(),
            ["pageWidth"] = pageWidth
        });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetAccountabilityPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/accountability/{accountabilityId}/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetUnserviceablePdfAsync(Guid reportId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/unserviceable/{reportId}/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetIncidentPdfAsync(Guid incidentReportId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/incidents/{incidentReportId}/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetPropertyCardPdfAsync(string propertyNo, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/property-card/pdf", new() { ["propertyNo"] = propertyNo, ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetIcsStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/ics/{accountabilityId}/stickers/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetParStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/par/{accountabilityId}/stickers/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

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

