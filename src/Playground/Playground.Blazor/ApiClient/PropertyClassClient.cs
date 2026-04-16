using System.Net.Http;
using System.Net.Http.Json;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;

namespace FSH.Playground.Blazor.ApiClient;

internal interface IPropertyClassClient
{
    Task<IReadOnlyList<PropertyClassDto>?> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropertyClassItemDto>?> GetItemsAsync(string? classCode = null, CancellationToken cancellationToken = default);
    Task<Guid> CreateClassAsync(CreatePropertyClassCommand command, CancellationToken cancellationToken = default);
    Task UpdateClassAsync(Guid id, UpdatePropertyClassCommand command, CancellationToken cancellationToken = default);
    Task<Guid> CreateItemAsync(Guid propertyClassId, CreatePropertyClassItemCommand command, CancellationToken cancellationToken = default);
    Task UpdateItemAsync(Guid id, UpdatePropertyClassItemCommand command, CancellationToken cancellationToken = default);
}

internal sealed class PropertyClassClient(HttpClient httpClient) : IPropertyClassClient
{
    private const string Base = "api/v1/master-data/property-classes";

    public Task<IReadOnlyList<PropertyClassDto>?> GetTreeAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<IReadOnlyList<PropertyClassDto>>($"{Base}/tree", cancellationToken);

    public Task<IReadOnlyList<PropertyClassItemDto>?> GetItemsAsync(string? classCode = null, CancellationToken cancellationToken = default)
    {
        var url = $"{Base}/items";
        if (!string.IsNullOrWhiteSpace(classCode))
            url += $"?classCode={Uri.EscapeDataString(classCode)}";
        return httpClient.GetFromJsonAsync<IReadOnlyList<PropertyClassItemDto>>(url, cancellationToken);
    }

    public async Task<Guid> CreateClassAsync(CreatePropertyClassCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(Base, command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: cancellationToken);
    }

    public async Task UpdateClassAsync(Guid id, UpdatePropertyClassCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"{Base}/{id}", command, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> CreateItemAsync(Guid propertyClassId, CreatePropertyClassItemCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"{Base}/{propertyClassId}/items", command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: cancellationToken);
    }

    public async Task UpdateItemAsync(Guid id, UpdatePropertyClassItemCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"{Base}/items/{id}", command, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
