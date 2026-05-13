using ZXing.Net.Maui;

namespace Playground.Maui.Features.PhysicalCount;

public partial class PhysicalCountScanPage : ContentPage
{
    private readonly PhysicalCountScanViewModel _vm;

    public PhysicalCountScanPage(PhysicalCountScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(async () => await _vm.OnBarcodeDetectedAsync(result.Value));
    }
}
