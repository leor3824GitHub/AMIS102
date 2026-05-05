using ZXing.Net.Maui;

namespace Playground.Maui.Features.Scan;

public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _vm;

    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(() => _vm.OnBarcodeDetected(result.Value));
    }
}
