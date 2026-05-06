using Playground.Maui.Services;
using ZXing.Net.Maui;

namespace Playground.Maui.Features.PhysicalCount;

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
        _ = _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            MainThread.BeginInvokeOnMainThread(() => _ = _vm.FlushPendingAsync());
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result is null) return;
        MainThread.BeginInvokeOnMainThread(() => _vm.OnBarcodeDetected(result.Value));
    }

    private void OnEntrySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PhysicalCountEntryDto entry)
        {
            ((CollectionView)sender).SelectedItem = null;
            _ = _vm.OpenEntryCommand.ExecuteAsync(entry);
        }
    }
}
