using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Features.Asset;

namespace Playground.Maui.Features.Scan;

public sealed partial class ScanViewModel : ObservableObject
{
    [ObservableProperty] private string _manualPropertyNo = "";
    [ObservableProperty] private bool _isCameraAvailable;

    private DateTimeOffset? _lastScanTime;

    public ScanViewModel()
    {
        IsCameraAvailable = DeviceInfo.Current.Platform != DevicePlatform.Unknown;
    }

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
