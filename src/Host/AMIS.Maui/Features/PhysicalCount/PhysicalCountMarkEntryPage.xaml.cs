namespace AMIS.Maui.Features.PhysicalCount;

public partial class PhysicalCountMarkEntryPage : ContentPage
{
    public PhysicalCountMarkEntryPage(PhysicalCountMarkEntryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
