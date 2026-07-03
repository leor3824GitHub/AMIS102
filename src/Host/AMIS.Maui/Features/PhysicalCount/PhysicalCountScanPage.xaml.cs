using System.ComponentModel;
using AMIS.Maui.Features.Shared;
using ZXing.Net.Maui;

namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountScanPage : ContentPage
{
    private readonly PhysicalCountScanViewModel _vm;
    private readonly LiveTextScanController _textScan;

    public PhysicalCountScanPage(PhysicalCountScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _textScan = new LiveTextScanController(
            TextCameraHost, _vm.ProcessTextFrameAsync, _vm.OnCameraPermissionDenied, _vm.OnTextScanUnavailable);
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        // Shell needs an explicit kick to (re)start ZXing detection after navigating away and back.
        BarcodeReader.IsDetecting = _vm.IsBarcodeMode && !_vm.IsConfirmSheetOpen;
        _ = _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        BarcodeReader.IsDetecting = false;
        _textScan.Stop();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PhysicalCountScanViewModel.IsTextMode):
                if (_vm.IsTextMode)
                {
                    // Release the camera before the CameraView takes it — one camera at a time.
                    BarcodeReader.IsDetecting = false;
                    _ = _textScan.StartAsync();
                }
                else
                {
                    _textScan.Stop();
                    BarcodeReader.IsDetecting = _vm.IsBarcodeMode && !_vm.IsConfirmSheetOpen;
                }
                break;

            case nameof(PhysicalCountScanViewModel.IsConfirmSheetOpen):
                // Pause the reader while the confirm sheet is up so it can't stack another scan behind it.
                BarcodeReader.IsDetecting = _vm.IsBarcodeMode && !_vm.IsConfirmSheetOpen && !_vm.IsTextMode;
                break;
        }
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(() => _vm.OnBarcodeDetected(result.Value));
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            MainThread.BeginInvokeOnMainThread(() => _ = _vm.FlushPendingAsync());
    }
}
