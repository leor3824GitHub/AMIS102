using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FSH.Modules.Expendable.Contracts.v1.Requests;

namespace FSH.Playground.Blazor.ApiClient;

public static class SupplyRequestClientExtensions
{
    /// <summary>Fulfill an approved supply request — issues from warehouse and records per-employee receipt</summary>
    public static async Task<FulfillSupplyRequestResponse> FulfillAsync(
        this ISupply_requestsClient client,
        System.Guid id,
        System.Guid? warehouseLocationId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var httpClient = GetHttpClient(client);

        var body = new { warehouseLocationId, notes };
        var response = await httpClient.PostAsJsonAsync(
            $"api/v1/expendable/supply-requests/{id}/fulfill",
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FulfillSupplyRequestResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Null response from fulfill endpoint.");
    }

    /// <summary>Cancel a supply request (Draft, Submitted, or Approved — not yet Fulfilled)</summary>
    public static async Task CancelAsync(
        this ISupply_requestsClient client,
        System.Guid id,
        CancellationToken cancellationToken = default)
    {
        var httpClient = GetHttpClient(client);
        var response = await httpClient.PutAsync(
            $"api/v1/expendable/supply-requests/{id}/cancel",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static HttpClient GetHttpClient(ISupply_requestsClient client)
    {
        if (client is not Supply_requestsClient impl)
            throw new InvalidOperationException("Client is not a Supply_requestsClient instance.");

        var field = impl.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_httpClient field not found on Supply_requestsClient.");

        return (HttpClient)(field.GetValue(impl)
            ?? throw new InvalidOperationException("_httpClient is null."));
    }
}
