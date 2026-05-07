using CommunityToolkit.Mvvm.Messaging;
using Playground.Maui.Features.Auth;
using Playground.Maui.Services;

namespace Playground.Maui;

public partial class App : Application
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly IPinStorageService _pinStorage;
    private readonly ICacheService _cacheService;
    private readonly AuthStateService _authState;
    private readonly ApiClientOptions _apiOptions;

    public App(
        ITokenStorageService tokenStorage,
        IPinStorageService pinStorage,
        ICacheService cacheService,
        AuthStateService authState,
        ApiClientOptions apiOptions)
    {
        InitializeComponent();
        _tokenStorage = tokenStorage;
        _pinStorage = pinStorage;
        _cacheService = cacheService;
        _authState = authState;
        _apiOptions = apiOptions;

        WeakReferenceMessenger.Default.Register<SessionExpiredMessage>(this, async (_, _) =>
            await OnSessionExpiredAsync());
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
        var (lastUserId, lastTenantId) = await _tokenStorage.GetLastSessionAsync();

        if (accessToken is not null)
        {
            // Token exists — restore auth state from cache if available
            if (lastUserId is not null)
                await TryRestoreAuthStateAsync(lastUserId, lastTenantId);

            MainPage = new AppShell();
            return;
        }

        // No token — check for PIN-based offline login
        if (lastUserId is not null && await _pinStorage.IsPinSetAsync())
        {
            var identity = await _cacheService.GetUserIdentityAsync(lastUserId);
            var employee = await _cacheService.GetEmployeeProfileAsync(lastUserId);
            if (identity is not null && employee is not null)
            {
                MainPage = new NavigationPage(ResolvePage<PinPage>());
                return;
            }
        }

        MainPage = new NavigationPage(ResolvePage<LoginPage>());
    }

    private async Task TryRestoreAuthStateAsync(string userId, string? tenantId)
    {
        var identity = await _cacheService.GetUserIdentityAsync(userId);
        var employee = await _cacheService.GetEmployeeProfileAsync(userId);
        if (identity is null || employee is null) return;

        _authState.SetUserProfile(new UserProfile(
            identity.UserId, identity.Email, identity.FirstName, identity.LastName, null));
        _authState.SetEmployee(new EmployeeInfo(
            employee.EmployeeId, employee.FullName, employee.Department, employee.Position));

        if (!string.IsNullOrEmpty(tenantId))
            _apiOptions.TenantId = tenantId;
    }

    private async Task OnSessionExpiredAsync()
    {
        _authState.Clear();

        var (lastUserId, _) = await _tokenStorage.GetLastSessionAsync();

        Page nextPage;
        if (lastUserId is not null && await _pinStorage.IsPinSetAsync())
        {
            var identity = await _cacheService.GetUserIdentityAsync(lastUserId);
            var employee = await _cacheService.GetEmployeeProfileAsync(lastUserId);
            nextPage = (identity is not null && employee is not null)
                ? new NavigationPage(ResolvePage<PinPage>())
                : new NavigationPage(ResolvePage<LoginPage>());
        }
        else
        {
            nextPage = new NavigationPage(ResolvePage<LoginPage>());
        }

        MainThread.BeginInvokeOnMainThread(() => MainPage = nextPage);
    }

    private T ResolvePage<T>() where T : Page =>
        (T)Handler!.MauiContext!.Services.GetRequiredService(typeof(T));
}
