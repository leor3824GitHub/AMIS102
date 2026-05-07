namespace Playground.Maui.Features.Auth;

public partial class SetPinPage : ContentPage
{
    public SetPinPage(SetPinViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
