using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Features.Asset;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Scan;

public sealed partial class ScanViewModel : ObservableObject
{
    private readonly IOcrService _ocr;

    [ObservableProperty] private string _manualPropertyNo = "";
    [ObservableProperty] private bool _isCameraAvailable;
    [ObservableProperty] private bool _isOcrBusy;
    [ObservableProperty] private string? _errorMessage;

    private DateTimeOffset? _lastScanTime;

    public ScanViewModel(IOcrService ocr)
    {
        _ocr = ocr;
        IsCameraAvailable = DeviceInfo.Current.Platform != DevicePlatform.Unknown;
    }

    public bool IsOcrSupported => _ocr.IsSupported;

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

    [RelayCommand]
    private async Task ScanTextAsync(CancellationToken ct)
    {
        if (IsOcrBusy) return;
        if (!_ocr.IsSupported)
        {
            ErrorMessage = "Text scanning isn't available on this device.";
            return;
        }

        IsOcrBusy = true;
        ErrorMessage = null;
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return;

            await using var stream = await photo.OpenReadAsync();
            var raw = await _ocr.RecognizeTextAsync(stream, ct);
            var info = PropertyNumberExtractor.Extract(raw);

            if (string.IsNullOrEmpty(info.PropertyNo))
            {
                ManualPropertyNo = (raw ?? string.Empty).Trim();
                ErrorMessage = "Couldn't detect a property number. Edit the text below and tap Search.";
                return;
            }

            ManualPropertyNo = info.PropertyNo;
            await NavigateToAssetAsync(info.PropertyNo);
        }
        catch (FeatureNotSupportedException)
        {
            ErrorMessage = "Camera capture isn't supported on this device.";
        }
        catch (PermissionException)
        {
            ErrorMessage = "Camera permission was denied.";
        }
        catch (Exception)
        {
            ErrorMessage = "Could not scan text. Please try again.";
        }
        finally
        {
            IsOcrBusy = false;
        }
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
