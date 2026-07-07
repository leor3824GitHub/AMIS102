namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountChecklistPage : ContentPage
{
    public PhysicalCountChecklistPage(PhysicalCountChecklistViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
