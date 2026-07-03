using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Features.Shared;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

// Record-as-you-go physical count. Inherits the shared capture layer (barcode / OCR / manual / serial)
// and adds the terminal action: resolve the property number against the registry, then open a compact
// confirm sheet where the operator sets condition + remarks and adds the entry at the selected location.
// It stays in scan mode after each add so the user can sweep sticker to sticker.
[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountScanViewModel : PropertyCaptureViewModel
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private List<PhysicalCountEntryDto> _allEntries = [];

    // Guards the resolve/confirm flow so the continuously-firing reader can't stack sheets, and
    // remembers the last item counted so it isn't re-prompted while still in the camera frame.
    private bool _isResolving;
    private string? _lastCountedPropertyNo;

    // The asset currently shown in the confirm sheet (set when a scan/lookup resolves a known asset).
    private TangibleInventoryItemDetailDto? _pendingAsset;
    private bool _pendingIsScanned;

    // Condition options for the confirm sheet → AssetRegister PhysicalCountCondition values.
    private static readonly string[] ConditionApiValues = ["InGoodCondition", "NeedingRepair", "Unserviceable"];
    public string[] Conditions => ["In Good Condition", "Needing Repair", "Unserviceable"];

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _sessionNo = "";
    [ObservableProperty] private string _fundCluster = "";
    [ObservableProperty] private string _scope = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRecord))]
    [NotifyPropertyChangedFor(nameof(ShowStatusLockedNotice))]
    private string _status = "";

    [ObservableProperty] private bool _isLoading;

    // "Counting at" location — required to record (AssetRegister stores where each item was counted).
    [ObservableProperty] private ObservableCollection<LocationDto> _locations = [];
    [ObservableProperty] private LocationDto? _selectedLocation;

    // Progress chips.
    [ObservableProperty] private int _foundCount;
    [ObservableProperty] private int _missingCount;
    [ObservableProperty] private int _foundAtStationCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingSync))]
    private int _pendingSyncCount;

    public bool HasPendingSync => PendingSyncCount > 0;

    // Feedback banner. StatusKind drives the colour ("Success" | "Warning" | "Error").
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string _statusKind = "Success";

    // Recording is only allowed while the session is Ongoing (the server enforces the same).
    public bool CanRecord => string.Equals(Status, "Ongoing", StringComparison.OrdinalIgnoreCase);
    public bool ShowStatusLockedNotice => !string.IsNullOrEmpty(Status) && !CanRecord;

    // ----- Confirm sheet state -----
    [ObservableProperty] private bool _isConfirmSheetOpen;
    [ObservableProperty] private string _sheetAssetName = "";
    [ObservableProperty] private string _sheetPropertyNo = "";
    [ObservableProperty] private string _sheetUnitCostText = "";
    [ObservableProperty] private string _sheetLocationText = "";
    [ObservableProperty] private int _selectedConditionIndex;
    [ObservableProperty] private string _sheetRemarks = "";
    [ObservableProperty] private bool _isSaving;

    public PhysicalCountScanViewModel(
        IApiClient apiClient, IPhysicalCountSyncService syncService, IOcrService ocr)
        : base(apiClient, ocr)
    {
        _apiClient = apiClient;
        _syncService = syncService;
        // The shared capture base reports serial-search / camera / OCR failures via ErrorMessage;
        // surface those in the on-screen feedback banner too (the page only binds StatusMessage).
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ErrorMessage) && !string.IsNullOrEmpty(ErrorMessage))
                SetStatus("Error", ErrorMessage);
        };
    }

    // ----- Capture policy (overrides of the shared base) -----

    public override string SearchPlaceholder => SearchBySerial
        ? "Serial number"
        : "Property No. (type or scan)";

    // Keep scanning after each record so the user can sweep sticker to sticker.
    protected override bool StopTextModeOnHit => false;

    // Don't re-prompt for the asset still sitting in the camera frame.
    protected override bool ShouldSkip(string propertyNo) =>
        string.Equals(propertyNo, _lastCountedPropertyNo, StringComparison.Ordinal);

    // Terminal action: resolve then open the confirm sheet. Barcode/OCR count as scanned; manual/serial as typed.
    protected override Task HandleResolvedPropertyNoAsync(string propertyNo, PropertyInputSource source) =>
        ProcessPropertyNoAsync(propertyNo, isScanned: source is PropertyInputSource.Barcode or PropertyInputSource.Ocr);

    partial void OnSessionIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(SessionId)) return;
        IsLoading = true;
        try
        {
            var detail = await _apiClient.GetPhysicalCountSessionByIdAsync(Guid.Parse(SessionId), ct);
            SessionNo = detail.SessionNo;
            FundCluster = detail.FundCluster;
            Scope = detail.Scope;
            Status = detail.Status;
            _allEntries = detail.Entries;
            UpdateCounts();

            if (Locations.Count == 0)
            {
                try
                {
                    var locs = await _apiClient.GetLocationsAsync(ct);
                    Locations = new ObservableCollection<LocationDto>(locs);
                    // Reduce friction: with a single location, pick it so the first scan can record right away.
                    if (Locations.Count == 1)
                        SelectedLocation = Locations[0];
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    // Non-fatal: locations didn't load. Picker will be empty and the first scan
                    // will surface "select a location" in the feedback banner.
                }
            }

            // Make a blocking data gap obvious instead of failing silently on every scan.
            if (CanRecord && Locations.Count == 0)
                SetStatus("Error", "No counting locations are available — recording can't proceed until one is added.");

            PendingSyncCount = await _syncService.GetPendingCountAsync();
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            SetStatus("Error", "You're offline. Recorded entries may be out of date.");
        }
        catch (HttpRequestException)
        {
            SetStatus("Error", "Couldn't load the session. Pull to refresh.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task FlushPendingAsync(CancellationToken ct = default)
    {
        var result = await _syncService.FlushPendingAsync(ct);
        PendingSyncCount = await _syncService.GetPendingCountAsync();
        if (result.Sent > 0 && result.Failed == 0)
            SetStatus("Success", $"Synced {result.Sent} queued entry(s).");
        else if (result.Failed > 0)
            SetStatus("Error", $"{result.Failed} entry(s) failed to sync: {result.LastError}");
        if (PendingSyncCount == 0)
            await LoadAsync(ct);
    }

    [RelayCommand]
    private Task OpenEntriesAsync() =>
        Shell.Current.GoToAsync($"{nameof(PhysicalCountEntriesPage)}?SessionId={SessionId}");

    // Resolve a scanned/typed property number against the asset registry, then either open the confirm
    // sheet for a known asset (record-as-you-go) or route an unknown one to the found-at-station screen.
    private async Task ProcessPropertyNoAsync(string propertyNo, bool isScanned)
    {
        if (_isResolving || IsConfirmSheetOpen) return;

        if (!CanRecord)
        {
            SetStatus("Error", $"This session is {Status} — recording is disabled.");
            return;
        }
        if (SelectedLocation is null)
        {
            SetStatus("Error", "Select where you are counting (location) before recording.");
            return;
        }

        _isResolving = true;
        StatusMessage = null;
        var location = SelectedLocation;
        try
        {
            TangibleInventoryItemDetailDto asset;
            try
            {
                asset = await _apiClient.GetItemByPropertyNoAsync(propertyNo);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Not in the registry → capture as found at station (full classify screen).
                // Remember it so the reader doesn't re-launch that screen while it's still in frame.
                _lastCountedPropertyNo = propertyNo;
                await Shell.Current.GoToAsync(
                    $"{nameof(PhysicalCountFoundAtStationPage)}" +
                    $"?SessionId={SessionId}" +
                    $"&PropertyNo={Uri.EscapeDataString(propertyNo)}" +
                    $"&LocationId={location.Id}");
                return;
            }

            // Populate and open the confirm sheet.
            _pendingAsset = asset;
            _pendingIsScanned = isScanned;
            SheetAssetName = string.IsNullOrWhiteSpace(asset.ItemName) ? asset.PropertyNo : asset.ItemName;
            SheetPropertyNo = asset.PropertyNo;
            SheetUnitCostText = $"₱{asset.UnitCost:N2}";
            SheetLocationText = location.Name;
            SelectedConditionIndex = 0;
            SheetRemarks = "";
            IsConfirmSheetOpen = true;
        }
        catch (HttpRequestException)
        {
            SetStatus("Error", "Couldn't check the registry. Verify your connection and try again.");
        }
        catch (Exception)
        {
            SetStatus("Error", "Something went wrong. Please try again.");
        }
        finally
        {
            _isResolving = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmAddAsync(CancellationToken ct = default)
    {
        if (_pendingAsset is not { } asset) return;
        if (SelectedLocation is not { } location)
        {
            SetStatus("Error", "Select a counting location.");
            return;
        }

        IsSaving = true;
        try
        {
            var request = new RecordCountEntryRequest(
                asset.Id,
                string.IsNullOrWhiteSpace(asset.ItemName) ? asset.PropertyNo : asset.ItemName,
                string.IsNullOrWhiteSpace(asset.Unit) ? "unit" : asset.Unit,
                asset.UnitCost,
                ConditionApiValues[SelectedConditionIndex],
                location.Id,
                string.IsNullOrWhiteSpace(SheetRemarks) ? null : SheetRemarks.Trim(),
                _pendingIsScanned);

            var result = await _syncService.RecordEntryAsync(Guid.Parse(SessionId), request, ct);
            switch (result.Status)
            {
                case RecordSaveStatus.SavedToServer:
                    CloseSheet(asset.PropertyNo);
                    SetStatus("Success", $"✓ Added {asset.PropertyNo} · {SheetAssetName}");
                    await LoadAsync(ct);
                    break;

                case RecordSaveStatus.QueuedOffline:
                    CloseSheet(asset.PropertyNo);
                    // Reflect the add immediately so the operator sees progress even offline.
                    FoundCount++;
                    PendingSyncCount = await _syncService.GetPendingCountAsync();
                    SetStatus("Warning", $"Queued {asset.PropertyNo} — will sync when you're back online.");
                    break;

                case RecordSaveStatus.Rejected:
                    // Keep the sheet open so the operator can fix condition/remarks or cancel.
                    SetStatus("Error", result.Error ?? "The server rejected this entry.");
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            SetStatus("Error", "Couldn't add the item. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Skip()
    {
        var propertyNo = _pendingAsset?.PropertyNo;
        CloseSheet(propertyNo);
    }

    private void CloseSheet(string? countedPropertyNo)
    {
        IsConfirmSheetOpen = false;
        _pendingAsset = null;
        ManualPropertyNo = "";
        if (!string.IsNullOrEmpty(countedPropertyNo))
            _lastCountedPropertyNo = countedPropertyNo.Trim().ToUpperInvariant();
    }

    private void SetStatus(string kind, string message)
    {
        StatusKind = kind;
        StatusMessage = message;
    }

    private void UpdateCounts()
    {
        FoundCount = _allEntries.Count(e => e.Result == "Found");
        MissingCount = _allEntries.Count(e => e.Result == "NotFound");
        FoundAtStationCount = _allEntries.Count(e => e.Result == "FoundAtStation");
    }
}
