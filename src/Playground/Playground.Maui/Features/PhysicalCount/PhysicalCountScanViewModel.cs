using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Playground.Maui.Features.PhysicalCount;

public sealed partial class PhysicalCountScanViewModel : ObservableObject
{
    [ObservableProperty] private bool _isCameraAvailable;

    private DateTimeOffset? _lastScanTime;
    private bool _hasReported;

    public PhysicalCountScanViewModel()
    {
        IsCameraAvailable = DeviceInfo.Current.Platform != DevicePlatform.Unknown;
    }

    public async Task OnBarcodeDetectedAsync(string rawValue)
    {
        if (_hasReported || IsDebounced()) return;
        var propertyNo = rawValue.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(propertyNo)) return;
        _hasReported = true;

        WeakReferenceMessenger.Default.Send(new PhysicalCountBarcodeScannedMessage(propertyNo));
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    private bool IsDebounced()
    {
        if (_lastScanTime.HasValue &&
            (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2)
            return true;
        _lastScanTime = DateTimeOffset.UtcNow;
        return false;
    }
}
