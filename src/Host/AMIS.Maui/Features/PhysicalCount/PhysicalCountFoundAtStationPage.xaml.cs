namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountFoundAtStationPage : ContentPage
{
    public PhysicalCountFoundAtStationPage(PhysicalCountFoundAtStationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
