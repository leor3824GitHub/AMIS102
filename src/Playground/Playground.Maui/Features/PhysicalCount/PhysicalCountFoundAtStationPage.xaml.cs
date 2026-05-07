namespace Playground.Maui.Features.PhysicalCount;

public partial class PhysicalCountFoundAtStationPage : ContentPage
{
    public PhysicalCountFoundAtStationPage(PhysicalCountFoundAtStationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
