using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(EntryId), "EntryId")]
[QueryProperty(nameof(PropertyNo), "PropertyNo")]
[QueryProperty(nameof(DisplayDescription), "Desc")]
[QueryProperty(nameof(IsScannedParam), "IsScanned")]
public sealed partial class PhysicalCountMarkEntryViewModel : ObservableObject
{
    private readonly IPhysicalCountSyncService _syncService;

    // Query params
    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _entryId = "";
    [ObservableProperty] private string _propertyNo = "";
    [ObservableProperty] private string _displayDescription = "";
    [ObservableProperty] private string _isScannedParam = "";

    // Form
    [ObservableProperty] private int _selectedResultIndex;
    [ObservableProperty] private int _selectedConditionIndex;
    [ObservableProperty] private int _quantityOnHand = 1;
    [ObservableProperty] private string _remarks = "";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Display labels shown in Picker
    public string[] Results => ["Found", "Not Found", "Found at Station"];
    public string[] Conditions => ["Good", "Needs Repair", "Unserviceable", "Obsolete", "No Longer Needed"];

    // Maps picker index → API string values
    private static readonly string[] ResultApiValues = ["Found", "NotFound", "FoundAtStation"];
    private static readonly string[] ConditionApiValues = ["Good", "NeedsRepair", "Unserviceable", "Obsolete", "NoLongerNeeded"];

    public PhysicalCountMarkEntryViewModel(IPhysicalCountSyncService syncService) =>
        _syncService = syncService;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(SessionId, out var sessionId) ||
            !Guid.TryParse(EntryId, out var entryId))
        {
            ErrorMessage = "Invalid session or entry ID.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var isScanned = string.Equals(IsScannedParam, "True", StringComparison.OrdinalIgnoreCase);
            var request = new RecordCountEntryRequest(
                ResultApiValues[SelectedResultIndex],
                ConditionApiValues[SelectedConditionIndex],
                QuantityOnHand,
                string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim(),
                isScanned);

            var synced = await _syncService.RecordEntryAsync(sessionId, entryId, request, ct);
            var message = synced
                ? "Entry saved."
                : "Entry saved offline — will sync when connected.";

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
