using System.ComponentModel;
using AMIS.Maui.Features.Shared;
using Microsoft.Extensions.DependencyInjection;
using ZXing.Net.Maui;

namespace AMIS.Maui.Features.Scan;

public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _vm;
    private readonly LiveTextScanController _textScan;

    public ScanPage()
        : this(ResolveViewModel())
    {
    }

    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _textScan = new LiveTextScanController(
            TextCameraHost, _vm.ProcessTextFrameAsync, _vm.OnCameraPermissionDenied, _vm.OnTextScanUnavailable);
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(() => _vm.OnBarcodeDetected(result.Value));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ScanViewModel.IsTextMode)) return;

        if (_vm.IsTextMode)
        {
            // Release the camera before the CameraView takes it — one camera at a time.
            BarcodeReader.IsDetecting = false;
            _ = _textScan.StartAsync();
        }
        else
        {
            _textScan.Stop();
            // Reclaim the camera once the CameraView is torn down.
            BarcodeReader.IsDetecting = _vm.IsBarcodeMode;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Shell tab pages need an explicit kick to restart ZXing detection after navigating
        // away and back — the binding-only approach misses the camera lifecycle event on Android.
        BarcodeReader.IsDetecting = _vm.IsBarcodeMode;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
        _textScan.Stop();
    }

    private static ScanViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ScanViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ScanViewModel from DI.");
}
