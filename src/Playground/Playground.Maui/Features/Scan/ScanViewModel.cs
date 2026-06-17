using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Features.Asset;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Scan;

public sealed partial class ScanViewModel : ObservableObject
{
    private readonly IOcrService _ocr;

    [ObservableProperty] private string _manualPropertyNo = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarcodeMode))]
    private bool _isCameraAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarcodeMode))]
    private bool _isTextMode;

    [ObservableProperty] private string? _errorMessage;

    private DateTimeOffset? _lastScanTime;
    private bool _navigated;
    private bool _isProcessingFrame;

    public ScanViewModel(IOcrService ocr)
    {
        _ocr = ocr;
        IsCameraAvailable = DeviceInfo.Current.Platform != DevicePlatform.Unknown;
    }

    public bool IsOcrSupported => _ocr.IsSupported;

    // The barcode (ZXing) camera and the live text (OCR) camera cannot hold the device at the
    // same time, so they are mutually exclusive: barcode is the default, text takes over on demand.
    public bool IsBarcodeMode => IsCameraAvailable && !IsTextMode;

    public void OnBarcodeDetected(string rawValue)
    {
        if (IsDebounced()) return;
        var propertyNo = rawValue.Trim().ToUpperInvariant();
        _ = NavigateToAssetAsync(propertyNo);
    }

    [RelayCommand]
    private async Task SearchManualAsync(CancellationToken ct)
    {
        var propertyNo = ManualPropertyNo.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(propertyNo)) return;
        await NavigateToAssetAsync(propertyNo);
    }

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
        _navigated = false;
        IsTextMode = true;
    }

    [RelayCommand]
    private void StopTextScan() => IsTextMode = false;

    // Called once per captured frame by the page's capture loop. Returns quietly on a miss so the
    // loop keeps scanning; on a hit it navigates and leaves text mode so the camera is released.
    public async Task ProcessTextFrameAsync(Stream image, CancellationToken ct)
    {
        if (_navigated || _isProcessingFrame) return;
        _isProcessingFrame = true;
        try
        {
            var raw = await _ocr.RecognizeTextAsync(image, ct);
            var propertyNo = PropertyNumberExtractor.ExtractFirst(raw);
            if (string.IsNullOrEmpty(propertyNo)) return;

            _navigated = true;
            // OCR completes on a thread-pool thread (RecognizeTextAsync uses ConfigureAwait(false)),
            // so every UI touch below — observable properties, leaving text mode, navigation — must
            // be marshalled back to the UI thread.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ManualPropertyNo = propertyNo;
                IsTextMode = false;
                await NavigateToAssetAsync(propertyNo);
            });
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

    private static async Task NavigateToAssetAsync(string propertyNo) =>
        await Shell.Current.GoToAsync($"{nameof(AssetDetailPage)}?PropertyNo={Uri.EscapeDataString(propertyNo)}");

    private bool IsDebounced()
    {
        if (_lastScanTime.HasValue &&
            (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2)
            return true;
        _lastScanTime = DateTimeOffset.UtcNow;
        return false;
    }
}
