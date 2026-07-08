using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Asset;

[QueryProperty(nameof(PropertyNo), "PropertyNo")]
public sealed partial class AssetDetailViewModel(IApiClient apiClient) : ObservableObject
{
    [ObservableProperty] public partial string PropertyNo { get; set; } = "";
    [ObservableProperty] public partial TangibleInventoryItemDetailDto? Item { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    partial void OnPropertyNoChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        var normalized = PropertyNo.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized)) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Item = await apiClient.GetItemByPropertyNoAsync(normalized, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ErrorMessage = $"No asset found for property number \"{normalized}\".";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load asset details. Check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
