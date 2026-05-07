using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(PropertyNo), "PropertyNo")]
[QueryProperty(nameof(Desc), "Desc")]
[QueryProperty(nameof(UnitCost), "UnitCost")]
public sealed partial class PhysicalCountFoundAtStationViewModel : ObservableObject
{
    private readonly IPhysicalCountSyncService _syncService;

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _propertyNo = "";

    // Form fields
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _unitCostText = "";

    // Pre-fill hooks: navigation params arrive after construction; mirror them into the form fields.
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
    [ObservableProperty] private int _selectedConditionIndex;
    [ObservableProperty] private string _remarks = "";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public string[] Conditions => ["Good", "Needs Repair", "Unserviceable", "Obsolete", "No Longer Needed"];

    private static readonly string[] ConditionApiValues =
        ["Good", "NeedsRepair", "Unserviceable", "Obsolete", "NoLongerNeeded"];

    public PhysicalCountFoundAtStationViewModel(IPhysicalCountSyncService syncService) =>
        _syncService = syncService;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(SessionId, out var sessionId))
        {
            ErrorMessage = "Invalid session ID.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        if (!decimal.TryParse(UnitCostText, out var unitCost) || unitCost < 0)
        {
            ErrorMessage = "Enter a valid unit cost (0 or greater).";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var propertyNo = PropertyNo.Trim().ToUpperInvariant();
            var request = new AddFoundAtStationRequest(
                propertyNo,
                Description.Trim(),
                unitCost,
                ConditionApiValues[SelectedConditionIndex],
                string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim());

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
