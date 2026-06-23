using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using AMIS.Playground.Blazor.ApiClient;

namespace AMIS.Playground.Blazor.Services;

internal interface ICapitalizationThresholdState
{
    CapitalizationThresholdDto? Threshold { get; }
    event Action? OnChanged;
    Task EnsureLoadedAsync();
    void Set(CapitalizationThresholdDto? threshold);
}

internal sealed class CapitalizationThresholdState(ICapitalizationThresholdClient client) : ICapitalizationThresholdState
{
    private Task? _loadTask;

    public CapitalizationThresholdDto? Threshold { get; private set; }
    public event Action? OnChanged;

    // Idempotent: loads once per circuit. Safe for the layout to warm and for pages to await during init.
    public Task EnsureLoadedAsync() => _loadTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try { Set(await client.GetActiveAsync()); }
        catch { /* non-critical — consumers degrade gracefully when null */ }
    }

    public void Set(CapitalizationThresholdDto? threshold)
    {
        Threshold = threshold;
        OnChanged?.Invoke();
    }
}
