namespace Playground.Maui.Features.Inventory;

public partial class PARDetailPage : ContentPage
{
    public PARDetailPage(PARDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
