using CommunityToolkit.Mvvm.Messaging;
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

        WeakReferenceMessenger.Default.Register<SessionExpiredMessage>(this, (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => MainPage = new NavigationPage(ResolvePage<LoginPage>())));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use a blank page initially; OnStart replaces it after the window is attached.
        // Creating AppShell here causes a Fragment 1.8.9 crash on Android because the
        // Activity view hierarchy isn't ready when Fragment eagerly tries to attach tab views.
        return new Window(new ContentPage()) { Title = "AMIS Mobile" };
    }

    protected override async void OnStart()
    {
        base.OnStart();

        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        MainPage = accessToken is not null
            ? new AppShell()
            : new NavigationPage(ResolvePage<LoginPage>());
    }

    private T ResolvePage<T>() where T : Page =>
        (T)Handler!.MauiContext!.Services.GetRequiredService(typeof(T));
}
