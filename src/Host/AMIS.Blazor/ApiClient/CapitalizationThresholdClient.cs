using System.Net.Http.Json;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

namespace AMIS.Playground.Blazor.ApiClient;

internal interface ICapitalizationThresholdClient
{
    Task<IReadOnlyList<CapitalizationThresholdDto>> GetAllAsync(CancellationToken ct = default);
    Task<CapitalizationThresholdDto?> GetActiveAsync(CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateCapitalizationThresholdCommand command, CancellationToken ct = default);
    Task<CapitalizationThresholdDto> UpdateAsync(Guid id, UpdateCapitalizationThresholdCommand command, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, CancellationToken ct = default);
}

internal sealed class CapitalizationThresholdClient(HttpClient http) : ICapitalizationThresholdClient
{
    private const string Base = "api/v1/master-data/capitalization-thresholds";

    public Task<IReadOnlyList<CapitalizationThresholdDto>> GetAllAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<CapitalizationThresholdDto>>(Base, ct)!;

    public Task<CapitalizationThresholdDto?> GetActiveAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<CapitalizationThresholdDto>($"{Base}/active", ct);

    public async Task<Guid> CreateAsync(CreateCapitalizationThresholdCommand command, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(Base, command, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task<CapitalizationThresholdDto> UpdateAsync(Guid id, UpdateCapitalizationThresholdCommand command, CancellationToken ct = default)
    {
        using var response = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CapitalizationThresholdDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Empty response.");
    }

    public async Task SetActiveAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.PatchAsync($"{Base}/{id}/set-active", null, ct);
        response.EnsureSuccessStatusCode();
    }
}

