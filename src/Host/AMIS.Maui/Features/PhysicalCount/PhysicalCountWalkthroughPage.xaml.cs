using AMIS.Maui.Services;
using ZXing.Net.Maui;

namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountWalkthroughPage : ContentPage
{
    private readonly PhysicalCountWalkthroughViewModel _vm;

    public PhysicalCountWalkthroughPage(PhysicalCountWalkthroughViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        _vm.SubscribeMessages();
        // Shell needs an explicit kick to (re)start ZXing detection after navigating away and back.
        BarcodeReader.IsDetecting = _vm.IsCameraAvailable;
        _ = _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        BarcodeReader.IsDetecting = false;
        _vm.UnsubscribeMessages();
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(async () => await _vm.OnBarcodeDetectedAsync(result.Value));
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            MainThread.BeginInvokeOnMainThread(() => _ = _vm.FlushPendingAsync());
    }

    // Recorded entries are read-only here; counting is record-as-you-go via scan / manual entry.
    private static void OnEntrySelected(object sender, SelectionChangedEventArgs e) =>
        ((CollectionView)sender).SelectedItem = null;
}
