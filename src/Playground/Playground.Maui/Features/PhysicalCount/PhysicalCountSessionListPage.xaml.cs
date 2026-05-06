using Playground.Maui.Services;

namespace Playground.Maui.Features.PhysicalCount;

public partial class PhysicalCountSessionListPage : ContentPage
{
    private readonly PhysicalCountSessionListViewModel _vm;

    public PhysicalCountSessionListPage(PhysicalCountSessionListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadAsync();
    }

    private void OnSessionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PhysicalCountSessionSummaryDto session)
        {
            ((CollectionView)sender).SelectedItem = null;
            _ = _vm.OpenSessionAsync(session);
        }
    }
}
