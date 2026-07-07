using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Shared;

// Shared input-capture layer for features that identify an asset by property number via barcode,
// live-text OCR, manual entry, or serial-number search. Subclasses supply only the terminal action
// (navigate vs. record) by overriding HandleResolvedPropertyNoAsync. The camera lifecycle itself
// lives in the page code-behind via LiveTextScanController — this class is pure input logic.
public abstract partial class PropertyCaptureViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IOcrService _ocr;

    [ObservableProperty] private string _manualPropertyNo = "";

    // Manual-entry mode: false = look up by property number (default), true = search by serial number.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
    private bool _searchBySerial;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarcodeMode))]
    private bool _isCameraAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarcodeMode))]
    private bool _isTextMode;

    [ObservableProperty] private string? _errorMessage;

    // Barcode/OCR readers fire continuously; these gate the flood.
    private DateTimeOffset? _lastScanTime;
    private bool _isProcessingFrame;

    // Set when an OCR frame produces a hit that ends text mode (Scan navigates away), so the loop's
    // in-flight frames don't fire again before it's torn down. Reset when text mode restarts.
    private bool _ocrHandled;

    protected PropertyCaptureViewModel(IApiClient api, IOcrService ocr)
    {
        _api = api;
        _ocr = ocr;
        IsCameraAvailable = DeviceInfo.Current.Platform == DevicePlatform.Android
                         || DeviceInfo.Current.Platform == DevicePlatform.iOS;
    }

    public bool IsOcrSupported => _ocr.IsSupported;

    // Subclasses may override the wording (e.g. Count adds "type or scan").
    public virtual string SearchPlaceholder => SearchBySerial
        ? "Serial number"
        : "Property No. e.g. SPLV-2026-01-0001";

    // The barcode (ZXing) camera and the live text (OCR) camera cannot hold the device at the
    // same time, so they are mutually exclusive: barcode is the default, text takes over on demand.
    public bool IsBarcodeMode => IsCameraAvailable && !IsTextMode;

    // Terminal action — navigate (Scan) or confirm+record (Count).
    protected abstract Task HandleResolvedPropertyNoAsync(string propertyNo, PropertyInputSource source);

    // After an OCR hit: Scan navigates away, so it exits text mode; Count stays to scan the next sticker
    // (relying on ShouldSkip + debounce to avoid re-recording the one still in frame).
    protected virtual bool StopTextModeOnHit => true;

    // Lets subclasses skip a just-handled property (e.g. Count won't re-count the sticker still in frame).
    protected virtual bool ShouldSkip(string propertyNo) => false;

    // ----- Barcode -----

    public void OnBarcodeDetected(string rawValue) =>
        _ = HandleDetectedAsync(rawValue, PropertyInputSource.Barcode);

    // ----- Manual / Serial -----

    [RelayCommand]
    private async Task SearchManualAsync(CancellationToken ct)
    {
        var raw = ManualPropertyNo.Trim();
        if (string.IsNullOrEmpty(raw)) return;

        if (SearchBySerial)
        {
            var propertyNo = await ResolveSerialToPropertyNoAsync(raw, ct);
            if (propertyNo is not null)
                await HandleDetectedAsync(propertyNo, PropertyInputSource.Serial);
            return;
        }

        await HandleDetectedAsync(raw, PropertyInputSource.Manual);
    }

    // Resolves a serial number to a single property number so the rest of the flow is unchanged:
    // none → inline error, one → straight through, many → a tappable action sheet to pick from.
    protected async Task<string?> ResolveSerialToPropertyNoAsync(string serial, CancellationToken ct)
    {
        ErrorMessage = null;
        IReadOnlyList<AssetSummaryDto> matches;
        try
        {
            matches = await _api.SearchAssetsBySerialAsync(serial, ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't search by serial number. Check your connection.";
            return null;
        }

        if (matches.Count == 0)
        {
            ErrorMessage = $"No asset found with serial number \"{serial}\".";
            return null;
        }
        if (matches.Count == 1)
            return matches[0].PropertyNo;

        var labels = matches.Select(m => $"{m.PropertyNo} - {m.Description}").ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"{matches.Count} assets match this serial", "Cancel", null, labels);
        var index = string.IsNullOrEmpty(choice) ? -1 : Array.IndexOf(labels, choice);
        return index >= 0 ? matches[index].PropertyNo : null;
    }

    // ----- OCR (live text) -----

    // Switches the viewfinder into live text mode. The page reacts to IsTextMode by starting the
    // capture loop and feeding each frame to ProcessTextFrameAsync — no shutter, no confirm step.
    [RelayCommand]
    private void StartTextScan()
    {
        if (!_ocr.IsSupported)
        {
            ErrorMessage = "Text scanning isn't available on this device.";
            return;
        }

        ErrorMessage = null;
        _ocrHandled = false;
        IsTextMode = true;
    }

    [RelayCommand]
    private void StopTextScan() => IsTextMode = false;

    // Called once per captured frame by the page's capture loop. Returns quietly on a miss so the
    // loop keeps scanning; on a hit it routes through the shared guarded dispatcher.
    public async Task ProcessTextFrameAsync(Stream image, CancellationToken ct)
    {
        if (_ocrHandled || _isProcessingFrame) return;
        _isProcessingFrame = true;
        try
        {
            var raw = await _ocr.RecognizeTextAsync(image, ct);
            var propertyNo = PropertyNumberExtractor.ExtractFirst(raw);
            if (string.IsNullOrEmpty(propertyNo)) return;

            // Scan ends text mode on a hit — latch so the loop's remaining in-flight frames don't
            // re-fire before the page tears the camera down. Count keeps scanning (no latch).
            if (StopTextModeOnHit) _ocrHandled = true;

            // OCR completes on a thread-pool thread (RecognizeTextAsync uses ConfigureAwait(false)),
            // so every UI touch below must be marshalled back to the UI thread.
            await MainThread.InvokeOnMainThreadAsync(() => HandleDetectedAsync(propertyNo, PropertyInputSource.Ocr));
        }
        catch (OperationCanceledException)
        {
            // Loop was cancelled (mode exit / page closed) — nothing to surface.
        }
        catch (Exception)
        {
            // A single bad frame must not stop live scanning; the next frame retries.
        }
        finally
        {
            _isProcessingFrame = false;
        }
    }

    public void OnCameraPermissionDenied()
    {
        IsTextMode = false;
        ErrorMessage = "Camera permission is needed to scan text. Enter the property number manually instead.";
    }

    public void OnTextScanUnavailable()
    {
        IsTextMode = false;
        ErrorMessage = "Live text scan couldn't start — no camera on this device. Enter the property number manually instead.";
    }

    // ----- Shared guarded dispatch -----

    // The single funnel for all four input sources: normalize, debounce camera sources, honor the
    // subclass skip rule, then hand off to the terminal action.
    private async Task HandleDetectedAsync(string rawValue, PropertyInputSource source)
    {
        var propertyNo = rawValue.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(propertyNo)) return;

        var isCameraSource = source is PropertyInputSource.Barcode or PropertyInputSource.Ocr;
        if (isCameraSource && IsDebounced()) return;
        // The skip rule exists to avoid re-firing on the sticker still sitting in the camera frame.
        // A deliberate manual/serial re-entry of the same number must always go through.
        if (isCameraSource && ShouldSkip(propertyNo)) return;

        ManualPropertyNo = propertyNo;
        if (StopTextModeOnHit)
            IsTextMode = false;

        await HandleResolvedPropertyNoAsync(propertyNo, source);
    }

    private bool IsDebounced()
    {
        if (_lastScanTime.HasValue &&
            (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2)
            return true;
        _lastScanTime = DateTimeOffset.UtcNow;
        return false;
    }
}
