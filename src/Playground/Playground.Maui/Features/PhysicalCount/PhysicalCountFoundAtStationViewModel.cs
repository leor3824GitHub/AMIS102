using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(PropertyNo), "PropertyNo")]
[QueryProperty(nameof(Desc), "Desc")]
[QueryProperty(nameof(UnitCost), "UnitCost")]
[QueryProperty(nameof(LocationIdParam), "LocationId")]
public sealed partial class PhysicalCountFoundAtStationViewModel : ObservableObject
{
    private readonly IPhysicalCountSyncService _syncService;
    private readonly IApiClient _apiClient;

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _propertyNo = "";
    [ObservableProperty] private string _locationIdParam = "";

    // Form fields
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _unitCostText = "";
    [ObservableProperty] private string _remarks = "";

    // Catalog search
    [ObservableProperty] private string _catalogSearchText = "";
    [ObservableProperty] private ObservableCollection<CatalogItemDto> _catalogResults = [];
    [ObservableProperty] private CatalogItemDto? _selectedCatalogItem;
    [ObservableProperty] private bool _isCatalogSearching;

    // Pre-fill hooks: navigation params arrive after construction; mirror them into form fields.
    public string Desc
    {
        set { if (!string.IsNullOrWhiteSpace(value)) Description = value; }
    }

    public string UnitCost
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                UnitCostText = parsed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public PhysicalCountFoundAtStationViewModel(IPhysicalCountSyncService syncService, IApiClient apiClient)
    {
        _syncService = syncService;
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task SearchCatalogAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(CatalogSearchText)) return;
        IsCatalogSearching = true;
        try
        {
            var results = await _apiClient.SearchCatalogItemsAsync(CatalogSearchText.Trim(), ct);
            CatalogResults = new ObservableCollection<CatalogItemDto>(results);
            if (results.Count == 0)
                ErrorMessage = "No catalog items matched. Try a different keyword.";
            else
                ErrorMessage = null;
        }
        catch (OperationCanceledException) { }
        catch { ErrorMessage = "Could not search catalog. Check your connection."; }
        finally { IsCatalogSearching = false; }
    }

    [RelayCommand]
    private void SelectCatalogItem(CatalogItemDto? item)
    {
        SelectedCatalogItem = item;
        CatalogResults = [];
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(SessionId, out var sessionId))
        {
            ErrorMessage = "Invalid session ID.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PropertyNo))
        {
            ErrorMessage = "Property No. is required. Scan the sticker or type it manually.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        if (!decimal.TryParse(UnitCostText, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var unitCost) || unitCost < 0)
        {
            ErrorMessage = "Enter a valid unit cost (0 or greater).";
            return;
        }

        if (!Guid.TryParse(LocationIdParam, out var locationId) || locationId == Guid.Empty)
        {
            ErrorMessage = "A counting location is required. Select it on the previous screen.";
            return;
        }

        if (SelectedCatalogItem is null)
        {
            ErrorMessage = "Select a catalog item — it is required for the session to be closed.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var propertyNo = PropertyNo.Trim().ToUpperInvariant();
            var unit = SelectedCatalogItem.DefaultUnit;
            var request = new AddFoundAtStationRequest(
                propertyNo,
                Description.Trim(),
                unit,
                unitCost,
                locationId,
                string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim(),
                SelectedCatalogItem.Id);

            var synced = await _syncService.AddFoundAtStationAsync(sessionId, request, ct);
            var message = synced
                ? "Asset added as Found at Station."
                : "Saved offline — will sync when connected.";

            await Shell.Current.DisplayAlert("Success", message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not save: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
