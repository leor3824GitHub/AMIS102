using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

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

    // Catalog search — live/incremental (debounced); matches catalog Code or Description server-side.
    [ObservableProperty] private string _catalogSearchText = "";
    [ObservableProperty] private ObservableCollection<CatalogItemDto> _catalogResults = [];
    [ObservableProperty] private CatalogItemDto? _selectedCatalogItem;
    [ObservableProperty] private bool _isCatalogSearching;
    [ObservableProperty] private bool _showNoResults;

    private CancellationTokenSource? _searchCts;

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

    // Live search: each keystroke restarts a short debounce, then queries. The newest keystroke
    // cancels the previous in-flight request so results never arrive out of order.
    partial void OnCatalogSearchTextChanged(string value) => _ = SearchCatalogAsync(value);

    // Bound to the keyboard Return key for users who prefer to submit explicitly.
    [RelayCommand]
    private Task SearchCatalogReturn() => SearchCatalogAsync(CatalogSearchText);

    private async Task SearchCatalogAsync(string keyword)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        var term = keyword?.Trim() ?? "";
        if (term.Length == 0)
        {
            CatalogResults = [];
            ShowNoResults = false;
            IsCatalogSearching = false;
            return;
        }

        try
        {
            await Task.Delay(350, cts.Token);
            IsCatalogSearching = true;
            ShowNoResults = false;
            var results = await _apiClient.SearchCatalogItemsAsync(term, cts.Token);
            if (cts.Token.IsCancellationRequested) return;

            CatalogResults = new ObservableCollection<CatalogItemDto>(results);
            ShowNoResults = results.Count == 0;
            ErrorMessage = null;
        }
        catch (OperationCanceledException) { }
        catch { ErrorMessage = "Could not search catalog. Check your connection."; }
        finally { if (_searchCts == cts) IsCatalogSearching = false; }
    }

    [RelayCommand]
    private void SelectCatalogItem(CatalogItemDto? item)
    {
        SelectedCatalogItem = item;
        CatalogResults = [];
        ShowNoResults = false;
        ErrorMessage = null;

        if (item is not null)
        {
            // Reduce typing: adopt the catalog description when the operator hasn't entered one.
            if (string.IsNullOrWhiteSpace(Description))
                Description = item.Description;
        }
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

            var result = await _syncService.AddFoundAtStationAsync(sessionId, request, ct);
            switch (result.Status)
            {
                case RecordSaveStatus.SavedToServer:
                    await Shell.Current.DisplayAlert("Success", "Asset added as Found at Station.", "OK");
                    await Shell.Current.GoToAsync("..");
                    break;
                case RecordSaveStatus.QueuedOffline:
                    await Shell.Current.DisplayAlert("Saved offline", "Saved offline — will sync when connected.", "OK");
                    await Shell.Current.GoToAsync("..");
                    break;
                case RecordSaveStatus.Rejected:
                    // The server refused it — keep the operator on the form to fix and retry.
                    ErrorMessage = result.Error ?? "The server rejected this entry.";
                    break;
            }
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
