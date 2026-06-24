using System.ComponentModel;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using ZXing.Net.Maui;

namespace AMIS.Maui.Features.Scan;

public partial class ScanPage : ContentPage
{
    // Cadence between live OCR frames. The loop is serial (next capture waits for the previous
    // capture + OCR), so this is added idle time on top of that — keeps CPU/battery in check.
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(700);

    private readonly ScanViewModel _vm;
    private CancellationTokenSource? _textScanCts;
    private CameraView? _textCamera;

    public ScanPage()
        : this(ResolveViewModel())
    {
    }

    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
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
            _ = StartTextScanLoopAsync();
        }
        else
        {
            StopTextScanLoop();
            // Reclaim the camera once the CameraView is torn down.
            BarcodeReader.IsDetecting = _vm.IsBarcodeMode;
        }
    }

    private async Task StartTextScanLoopAsync()
    {
        StopTextScanLoop();

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            _vm.OnCameraPermissionDenied();
            return;
        }

        _textScanCts = new CancellationTokenSource();
        var ct = _textScanCts.Token;

        try
        {
            // Realize the camera only now — on a device without one this throws here (caught) instead
            // of crashing the whole page at load time.
            _textCamera = new CameraView();
            TextCameraHost.Add(_textCamera);
            await _textCamera.StartCameraPreview(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            // No usable camera on this device — surface a hint and fall back to manual entry.
            _vm.OnTextScanUnavailable();
            StopTextScanLoop();
            return;
        }

        await RunTextScanLoopAsync(ct);
    }

    private async Task RunTextScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var camera = _textCamera;
            if (camera is null) break;

            try
            {
                // CaptureImage touches the camera control, so it must run on the UI thread — earlier
                // iterations can resume on a thread-pool thread after the OCR step's ConfigureAwait(false).
                using var frame = await MainThread.InvokeOnMainThreadAsync(() => camera.CaptureImage(ct));
                await _vm.ProcessTextFrameAsync(frame, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Transient capture/decode failure — keep scanning on the next frame.
            }

            try
            {
                await Task.Delay(FrameInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void StopTextScanLoop()
    {
        if (_textScanCts is not null)
        {
            _textScanCts.Cancel();
            _textScanCts.Dispose();
            _textScanCts = null;
        }

        if (_textCamera is not null)
        {
            try
            {
                _textCamera.StopCameraPreview();
            }
            catch (Exception)
            {
                // Preview may already be stopped/torn down — safe to ignore.
            }

            TextCameraHost.Remove(_textCamera);
            _textCamera.Handler?.DisconnectHandler();
            _textCamera = null;
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
        StopTextScanLoop();
    }

    private static ScanViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ScanViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ScanViewModel from DI.");
}
