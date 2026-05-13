using ZXing.Net.Maui;
using Microsoft.Extensions.DependencyInjection;

namespace Playground.Maui.Features.Scan;

public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _vm;

    public ScanPage()
        : this(ResolveViewModel())
    {
    }

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

    private static ScanViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ScanViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ScanViewModel from DI.");
}
