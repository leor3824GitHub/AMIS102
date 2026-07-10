using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;

namespace AMIS.Blazor.Services.AssetRegister;

public interface IAssetRegisterReportsClient
{
    Task<RegSpiReportDto?> GetRegSpiReportAsync(DateOnly asOfDate, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRegSpiFundClustersAsync(CancellationToken cancellationToken = default);
    Task<RegPpeiReportDto?> GetRegPpeiReportAsync(DateOnly asOfDate, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRegPpeiFundClustersAsync(CancellationToken cancellationToken = default);
    Task<RspiReportDto?> GetRspiReportAsync(DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool activeOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<RpiReportDto?> GetRpiReportAsync(DateOnly? dateFrom, DateOnly? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<RspiReportDto?> GetRspiReportAllAsync(DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool activeOnly, CancellationToken cancellationToken = default);
    Task<RpiReportDto?> GetRpiReportAllAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<PhysicalCountReportDto?> GetPhysicalCountReportAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IssuanceReportDocumentDto?> GetIssuanceReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<AccountabilityReportDto?> GetAccountabilityReportAsync(Guid accountabilityId, CancellationToken cancellationToken = default);
    Task<IncidentReportDocumentDto?> GetIncidentReportAsync(Guid incidentReportId, CancellationToken cancellationToken = default);
    Task<UnserviceableReportDocumentDto?> GetUnserviceableReportAsync(Guid reportId, CancellationToken cancellationToken = default);

    // ── PDF (QuestPDF) ──────────────────────────────────────────────────────
    Task<byte[]> GetPhysicalCountPdfAsync(Guid sessionId, bool ppe, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetRegSpiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetRegPpeiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetRspiPdfAsync(DateOnly? dateFrom = null, DateOnly? dateTo = null, AssetType? assetType = null, bool activeOnly = true, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetRpiPdfAsync(DateOnly? dateFrom = null, DateOnly? dateTo = null, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetPtrPdfAsync(Guid reportId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetAccountabilityPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetUnserviceablePdfAsync(Guid reportId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetIncidentPdfAsync(Guid incidentReportId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetPropertyCardPdfAsync(string propertyNo, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetIcsStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetParStickersPdfAsync(Guid accountabilityId, string? pageWidth = null, CancellationToken cancellationToken = default);
}

public sealed class AssetRegisterReportsClient(HttpClient httpClient) : IAssetRegisterReportsClient
{
    // The API serializes all enums as strings (global JsonStringEnumConverter in AMIS.Api/Program.cs).
    // GetFromJsonAsync's default web options do NOT include that converter, so enum fields like
    // AccountabilityStatus must be deserialized with these options or conversion fails.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<RegSpiReportDto?> GetRegSpiReportAsync(
        DateOnly asOfDate,
        Guid? custodianId = null,
        string? fundCluster = null,
        string? propertyClass = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/regspi", new Dictionary<string, string?>
        {
            ["asOfDate"] = asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString(),
            ["fundCluster"] = fundCluster,
            ["propertyClass"] = propertyClass
        });

        return httpClient.GetFromJsonAsync<RegSpiReportDto>(url, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRegSpiFundClustersAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<string>>(
            "api/v1/asset-register/reports/regspi/fund-clusters", JsonOptions, cancellationToken);
        return result ?? [];
    }

    public Task<RegPpeiReportDto?> GetRegPpeiReportAsync(
        DateOnly asOfDate,
        Guid? custodianId = null,
        string? fundCluster = null,
        string? propertyClass = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/regppei", new Dictionary<string, string?>
        {
            ["asOfDate"] = asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString(),
            ["fundCluster"] = fundCluster,
            ["propertyClass"] = propertyClass
        });

        return httpClient.GetFromJsonAsync<RegPpeiReportDto>(url, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRegPpeiFundClustersAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<string>>(
            "api/v1/asset-register/reports/regppei/fund-clusters", JsonOptions, cancellationToken);
        return result ?? [];
    }

    public Task<RspiReportDto?> GetRspiReportAsync(
        DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool activeOnly, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/rspi", new Dictionary<string, string?>
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["assetType"] = assetType?.ToString(),
            ["activeOnly"] = activeOnly ? "true" : "false",
            ["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });

        return httpClient.GetFromJsonAsync<RspiReportDto>(url, JsonOptions, cancellationToken);
    }

    public Task<RpiReportDto?> GetRpiReportAsync(
        DateOnly? dateFrom, DateOnly? dateTo, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/rpi", new Dictionary<string, string?>
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });

        return httpClient.GetFromJsonAsync<RpiReportDto>(url, JsonOptions, cancellationToken);
    }

    public Task<RspiReportDto?> GetRspiReportAllAsync(
        DateOnly? dateFrom, DateOnly? dateTo, AssetType? assetType, bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        // Reports need the whole dataset — one unpaged call instead of a magic pageSize:1000.
        var url = BuildUrl("api/v1/asset-register/reports/rspi", new Dictionary<string, string?>
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["assetType"] = assetType?.ToString(),
            ["activeOnly"] = activeOnly ? "true" : "false",
            ["all"] = "true"
        });

        return httpClient.GetFromJsonAsync<RspiReportDto>(url, JsonOptions, cancellationToken);
    }

    public Task<RpiReportDto?> GetRpiReportAllAsync(
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/asset-register/reports/rpi", new Dictionary<string, string?>
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["all"] = "true"
        });

        return httpClient.GetFromJsonAsync<RpiReportDto>(url, JsonOptions, cancellationToken);
    }

    public Task<PhysicalCountReportDto?> GetPhysicalCountReportAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<PhysicalCountReportDto>($"api/v1/asset-register/reports/count/{sessionId}", JsonOptions, cancellationToken);

    public Task<IssuanceReportDocumentDto?> GetIssuanceReportAsync(Guid reportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<IssuanceReportDocumentDto>($"api/v1/asset-register/reports/issuance/{reportId}", JsonOptions, cancellationToken);

    public Task<AccountabilityReportDto?> GetAccountabilityReportAsync(Guid accountabilityId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<AccountabilityReportDto>($"api/v1/asset-register/reports/accountability/{accountabilityId}", JsonOptions, cancellationToken);

    public Task<IncidentReportDocumentDto?> GetIncidentReportAsync(Guid incidentReportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<IncidentReportDocumentDto>($"api/v1/asset-register/reports/incidents/{incidentReportId}", JsonOptions, cancellationToken);

    public Task<UnserviceableReportDocumentDto?> GetUnserviceableReportAsync(Guid reportId, CancellationToken cancellationToken = default)
        => httpClient.GetFromJsonAsync<UnserviceableReportDocumentDto>($"api/v1/asset-register/reports/unserviceable/{reportId}", JsonOptions, cancellationToken);

    private const string PdfBase = "api/v1/quest-pdf-reporting/asset-register";

    public Task<byte[]> GetPhysicalCountPdfAsync(Guid sessionId, bool ppe, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var doc = ppe ? "rpcppe" : "rpcsemex";
        var url = BuildUrl($"{PdfBase}/physical-count/{sessionId}/{doc}/pdf", new() { ["pageWidth"] = pageWidth });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetRegSpiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/regspi/pdf", new()
        {
            ["asOfDate"] = asOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString(),
            ["fundCluster"] = fundCluster,
            ["propertyClass"] = propertyClass,
            ["pageWidth"] = pageWidth
        });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetRegPpeiPdfAsync(DateOnly? asOfDate = null, Guid? custodianId = null, string? fundCluster = null, string? propertyClass = null, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/regppei/pdf", new()
        {
            ["asOfDate"] = asOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["custodianId"] = custodianId?.ToString(),
            ["fundCluster"] = fundCluster,
            ["propertyClass"] = propertyClass,
            ["pageWidth"] = pageWidth
        });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetRspiPdfAsync(
        DateOnly? dateFrom = null, DateOnly? dateTo = null, AssetType? assetType = null, bool activeOnly = true,
        string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/rspi/pdf", new()
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["assetType"] = assetType?.ToString(),
            ["activeOnly"] = activeOnly ? "true" : "false",
            ["pageWidth"] = pageWidth
        });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetRpiPdfAsync(
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/rpi/pdf", new()
        {
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["pageWidth"] = pageWidth
        });
        return httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    public Task<byte[]> GetPtrPdfAsync(Guid reportId, string? pageWidth = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{PdfBase}/issuance/{reportId}/ptr/pdf", new() { ["pageWidth"] = pageWidth });
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

