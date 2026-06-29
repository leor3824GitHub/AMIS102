using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AMIS.Framework.Shared.Inspections;

namespace AMIS.Blazor.ApiClient;

/// <summary>
/// Hand-written typed client for the per-module "pending-for-me" inspection endpoints. Each module returns the
/// shared <see cref="PendingInspectionItem"/> shape; the unified worklist (<c>MyInspectionsPage</c>) merges them.
/// Uses the same authenticated <see cref="HttpClient"/> as the other manual clients (bearer + tenant header
/// added by the pipeline). Mirrors <see cref="NotificationsClient"/>.
/// </summary>
internal interface IInspectionWorklistClient
{
    Task<IReadOnlyList<PendingInspectionItem>> GetMyPendingProcurementAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PendingInspectionItem>> GetMyPendingReturnedPropertyAsync(CancellationToken ct = default);
}

// Named *Worklist* to avoid colliding with the NSwag-generated InspectionsClient in Generated.cs
// (a separate "purchase inspections" feature).
internal sealed class InspectionWorklistClient : IInspectionWorklistClient
{
    // Match the API's web JSON contract (camelCase, case-insensitive).
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public InspectionWorklistClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<PendingInspectionItem>> GetMyPendingProcurementAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<PendingInspectionItem>>(
            "api/v1/procurement/inspections/pending-for-me", Json, ct).ConfigureAwait(false) ?? [];

    public async Task<IReadOnlyList<PendingInspectionItem>> GetMyPendingReturnedPropertyAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<PendingInspectionItem>>(
            "api/v1/asset-register/inspections/pending-for-me", Json, ct).ConfigureAwait(false) ?? [];
}
