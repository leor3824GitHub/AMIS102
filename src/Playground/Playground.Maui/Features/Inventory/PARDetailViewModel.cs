using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Inventory;

[QueryProperty(nameof(Id), "Id")]
public sealed partial class PARDetailViewModel(IApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private PARDetailDto? _detail;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    partial void OnIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(Id, out var guid)) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Detail = await apiClient.GetPARByIdAsync(guid, ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load PAR details. Check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
