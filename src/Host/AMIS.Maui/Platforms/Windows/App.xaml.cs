using Microsoft.UI.Xaml;

namespace AMIS.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => AMIS.Maui.MauiProgram.CreateMauiApp();
}
