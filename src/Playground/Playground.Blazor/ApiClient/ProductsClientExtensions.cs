using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Playground.Blazor.ApiClient;

public static class ProductsClientExtensions
{
    public static async Task DiscontinueAsync(
        this IProductsClient client,
        System.Guid id,
        CancellationToken cancellationToken = default)
    {
        if (client is not ProductsClient productsClient)
            return;

        var httpClient = productsClient.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(productsClient) as HttpClient;

        if (httpClient == null)
            return;

        var response = await httpClient.PutAsync($"api/v1/expendable/products/{id}/discontinue", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static async Task MarkOutOfStockAsync(
        this IProductsClient client,
        System.Guid id,
        CancellationToken cancellationToken = default)
    {
        if (client is not ProductsClient productsClient)
            return;

        var httpClient = productsClient.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(productsClient) as HttpClient;

        if (httpClient == null)
            return;

        var response = await httpClient.PutAsync($"api/v1/expendable/products/{id}/out-of-stock", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
