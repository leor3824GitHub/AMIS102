using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ZXing.Net.Maui;

namespace Playground.Maui.Features.Scan;

public partial class ScanPage : ContentPage
{
    // Cadence between live OCR frames. The loop is serial (next capture waits for the previous
    // capture + OCR), so this is added idle time on top of that — keeps CPU/battery in check.
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(700);

    private readonly ScanViewModel _vm;
    private CancellationTokenSource? _textScanCts;

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
            _ = StartTextScanLoopAsync();
        else
            StopTextScanLoop();
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
            await TextCamera.StartCameraPreview(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            // Preview may not be ready yet; the capture loop retries each frame regardless.
        }

        await RunTextScanLoopAsync(ct);
    }

    private async Task RunTextScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // CaptureImage touches the camera control, so it must run on the UI thread — earlier
                // iterations can resume on a thread-pool thread after the OCR step's ConfigureAwait(false).
                using var frame = await MainThread.InvokeOnMainThreadAsync(() => TextCamera.CaptureImage(ct));
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

        try
        {
            TextCamera.StopCameraPreview();
        }
        catch (Exception)
        {
            // Preview may already be stopped/torn down — safe to ignore.
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTextScanLoop();
    }

    private static ScanViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ScanViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ScanViewModel from DI.");
}
