using Microsoft.UI.Xaml;

namespace Playground.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => Playground.Maui.MauiProgram.CreateMauiApp();
}
