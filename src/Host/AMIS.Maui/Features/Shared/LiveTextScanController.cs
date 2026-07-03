using CommunityToolkit.Maui.Views;

namespace AMIS.Maui.Features.Shared;

// Owns the live-text OCR camera loop: realizes a CameraView on demand inside a host layout, captures
// frames on a cadence, and hands each to a processing delegate. Lifted out of the Scan page so both
// Scan and Count share one implementation. The page still owns the ZXing barcode reader and decides
// when to pause/resume it — one camera at a time.
public sealed class LiveTextScanController
{
    // Cadence between live OCR frames. The loop is serial (next capture waits for the previous
    // capture + OCR), so this is added idle time on top of that — keeps CPU/battery in check.
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(700);

    private readonly Layout _host;
    private readonly Func<Stream, CancellationToken, Task> _processFrame;
    private readonly Action _onPermissionDenied;
    private readonly Action _onUnavailable;

    private CancellationTokenSource? _cts;
    private CameraView? _camera;

    public LiveTextScanController(
        Layout host,
        Func<Stream, CancellationToken, Task> processFrame,
        Action onPermissionDenied,
        Action onUnavailable)
    {
        _host = host;
        _processFrame = processFrame;
        _onPermissionDenied = onPermissionDenied;
        _onUnavailable = onUnavailable;
    }

    public async Task StartAsync()
    {
        Stop();

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            _onPermissionDenied();
            return;
        }

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            // Realize the camera only now — on a device without one this throws here (caught) instead
            // of crashing the whole page at load time.
            _camera = new CameraView();
            _host.Add(_camera);
            await _camera.StartCameraPreview(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            // No usable camera on this device — surface a hint and fall back to manual entry.
            _onUnavailable();
            Stop();
            return;
        }

        await RunLoopAsync(ct);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var camera = _camera;
            if (camera is null) break;

            try
            {
                // CaptureImage touches the camera control, so it must run on the UI thread — earlier
                // iterations can resume on a thread-pool thread after the OCR step's ConfigureAwait(false).
                using var frame = await MainThread.InvokeOnMainThreadAsync(() => camera.CaptureImage(ct));
                await _processFrame(frame, ct);
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

    public void Stop()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        if (_camera is not null)
        {
            try
            {
                _camera.StopCameraPreview();
            }
            catch (Exception)
            {
                // Preview may already be stopped/torn down — safe to ignore.
            }

            _host.Remove(_camera);
            _camera.Handler?.DisconnectHandler();
            _camera = null;
        }
    }
}
