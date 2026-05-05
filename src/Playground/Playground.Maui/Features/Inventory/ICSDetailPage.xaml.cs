namespace Playground.Maui.Features.Inventory;

public partial class ICSDetailPage : ContentPage
{
    public ICSDetailPage(ICSDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
