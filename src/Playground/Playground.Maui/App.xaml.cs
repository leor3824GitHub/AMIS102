using Playground.Maui.Features.Auth;
using Playground.Maui.Services;

namespace Playground.Maui;

public partial class App : Application
{
    private readonly ITokenStorageService _tokenStorage;

    public App(ITokenStorageService tokenStorage)
    {
        InitializeComponent();
        _tokenStorage = tokenStorage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Title = "AMIS Mobile" };
    }

    protected override async void OnStart()
    {
        base.OnStart();

        var token = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            MainPage = new NavigationPage(new LoginPage(
                (LoginViewModel)Handler!.MauiContext!.Services.GetRequiredService(typeof(LoginViewModel))));
        }
        else
        {
            MainPage = new AppShell();
        }
    }
}
