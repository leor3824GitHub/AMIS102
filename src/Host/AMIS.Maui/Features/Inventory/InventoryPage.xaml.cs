using Microsoft.Extensions.DependencyInjection;

namespace Playground.Maui.Features.Inventory;

public partial class InventoryPage : ContentPage
{
    private readonly InventoryViewModel _vm;

    public InventoryPage()
        : this(ResolveViewModel())
    {
    }

    public InventoryPage(InventoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private static InventoryViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<InventoryViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve InventoryViewModel from DI.");
}
