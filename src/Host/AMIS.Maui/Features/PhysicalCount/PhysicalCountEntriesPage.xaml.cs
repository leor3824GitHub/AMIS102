namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountEntriesPage : ContentPage
{
    public PhysicalCountEntriesPage(PhysicalCountEntriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
