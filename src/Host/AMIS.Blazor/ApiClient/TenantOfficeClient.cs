using System.Net.Http.Json;
using System.Text.Json;

namespace AMIS.Blazor.ApiClient;

/// <summary>
/// Links a tenant to the MasterData office it represents. Hand-written rather than regenerated through
/// NSwag, matching the raw-HttpClient approach the rest of the newer clients use.
/// </summary>
internal interface ITenantOfficeClient
{
    Task LinkAsync(string tenantId, Guid officeId, string? officeCode, CancellationToken ct = default);
}

internal sealed class TenantOfficeClient(HttpClient http) : ITenantOfficeClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task LinkAsync(string tenantId, Guid officeId, string? officeCode, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync(
            $"api/v1/tenants/{Uri.EscapeDataString(tenantId)}/office",
            new { tenantId, officeId, officeCode },
            JsonOptions,
            ct);

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ExtractErrorAsync(resp, ct));
        }
    }

    /// <summary>Pulls the API's problem-details message out, falling back to the status line.</summary>
    private static async Task<string> ExtractErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
            {
                return $"Request failed ({(int)response.StatusCode}).";
            }

            using var doc = JsonDocument.Parse(body);
            foreach (var name in (string[])["detail", "title", "message"])
            {
                if (doc.RootElement.TryGetProperty(name, out var value) &&
                    value.GetString() is { Length: > 0 } text)
                {
                    return text;
                }
            }

            return body;
        }
        catch (JsonException)
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }
}
