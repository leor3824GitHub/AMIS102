using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(AssetRegistryId), "AssetRegistryId")]
[QueryProperty(nameof(PropertyNo), "PropertyNo")]
[QueryProperty(nameof(DisplayDescription), "Desc")]
[QueryProperty(nameof(UnitParam), "Unit")]
[QueryProperty(nameof(UnitCostParam), "UnitCost")]
[QueryProperty(nameof(LocationIdParam), "LocationId")]
[QueryProperty(nameof(IsScannedParam), "IsScanned")]
public sealed partial class PhysicalCountMarkEntryViewModel : ObservableObject
{
    private readonly IPhysicalCountSyncService _syncService;

    // Query params
    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _assetRegistryId = "";
    [ObservableProperty] private string _propertyNo = "";
    [ObservableProperty] private string _displayDescription = "";
    [ObservableProperty] private string _unitParam = "unit";
    [ObservableProperty] private string _unitCostParam = "0";
    [ObservableProperty] private string _locationIdParam = "";
    [ObservableProperty] private string _isScannedParam = "";

    // Form
    [ObservableProperty] private int _selectedConditionIndex;
    [ObservableProperty] private string _remarks = "";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Condition picker → AssetRegister PhysicalCountCondition values.
    public string[] Conditions => ["In Good Condition", "Needing Repair", "Unserviceable"];
    private static readonly string[] ConditionApiValues = ["InGoodCondition", "NeedingRepair", "Unserviceable"];

    public PhysicalCountMarkEntryViewModel(IPhysicalCountSyncService syncService) =>
        _syncService = syncService;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(SessionId, out var sessionId) ||
            !Guid.TryParse(AssetRegistryId, out var assetRegistryId))
        {
            ErrorMessage = "Invalid session or asset ID.";
            return;
        }
        if (!Guid.TryParse(LocationIdParam, out var locationId) || locationId == Guid.Empty)
        {
            ErrorMessage = "A counting location is required.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var isScanned = string.Equals(IsScannedParam, "True", StringComparison.OrdinalIgnoreCase);
            _ = decimal.TryParse(UnitCostParam, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var unitCost);

            var request = new RecordCountEntryRequest(
                assetRegistryId,
                string.IsNullOrWhiteSpace(DisplayDescription) ? PropertyNo : DisplayDescription,
                string.IsNullOrWhiteSpace(UnitParam) ? "unit" : UnitParam,
                unitCost,
                ConditionApiValues[SelectedConditionIndex],
                locationId,
                string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim(),
                isScanned);

            var synced = await _syncService.RecordEntryAsync(sessionId, request, ct);
            var message = synced
                ? "Count recorded."
                : "Recorded offline — will sync when connected.";

            await Shell.Current.DisplayAlert("Saved", message, "OK");
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
