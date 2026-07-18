using CommunityToolkit.Mvvm.Messaging;
using AMIS.Maui.Features.Auth;
using AMIS.Maui.Services;

namespace AMIS.Maui;

public partial class App : Application
{
    private readonly ITokenStorageService _tokenStorage;

    public App(ITokenStorageService tokenStorage)
    {
        InitializeComponent();
        _tokenStorage = tokenStorage;

        WeakReferenceMessenger.Default.Register<SessionExpiredMessage>(this, (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => SetRootPage(new NavigationPage(ResolvePage<LoginPage>()))));
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
        if (accessToken is null)
        {
            SetRootPage(new NavigationPage(ResolvePage<LoginPage>()));
            return;
        }

        var authState = Resolve<AuthStateService>();

        // AuthStateService is in-memory and starts empty every launch. Restore the cached identity
        // synchronously (no network) so a token-resume shows the real user instantly and offline —
        // rather than blocking the shell on a startup HTTP call that can fail transiently (cold HTTP
        // stack, expired token mid-refresh) and leave Home greeting "there" with empty counts.
        if (authState.Restore())
        {
            SetRootPage(new AppShell());
            _ = RefreshIdentityInBackgroundAsync();   // freshen best-effort; never blocks the shell
            return;
        }

        // No cached identity yet (first resume after installing this change, or a cleared cache):
        // fetch it once before deciding. On a hard rejection the token is cleared → go to login.
        SetRootPage(await TryRehydrateSessionAsync()
            ? new AppShell()
            : new NavigationPage(ResolvePage<LoginPage>()));
    }

    /// <summary>
    /// Swaps the root page of the single application window. Replaces the deprecated
    /// <c>Application.MainPage</c> setter; this app creates exactly one window (see
    /// <see cref="CreateWindow"/>), so window 0 is always the one to update.
    /// </summary>
    public void SetRootPage(Page page)
    {
        if (Windows.Count > 0)
        {
            Windows[0].Page = page;
            return;
        }

        // Should be unreachable — OnStart and SessionExpired both run after the window is attached.
        System.Diagnostics.Debug.WriteLine("[App] SetRootPage called before a window existed; ignoring.");
    }

    /// <summary>
    /// Refreshes identity from the API without blocking navigation. Used when the shell is already
    /// shown from cache. A hard token rejection is handled by <see cref="AuthenticatedHttpHandler"/>
    /// (clears tokens + fires <c>SessionExpired</c> → this app navigates to login); anything else is
    /// a best-effort miss the user never sees.
    /// </summary>
    private async Task RefreshIdentityInBackgroundAsync()
    {
        try
        {
            await FetchIdentityAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Background identity refresh failed: {ex}");
        }
    }

    /// <summary>
    /// Fetches identity from the API and caches it. Returns true when the shell should be shown. On
    /// failure it distinguishes a dead session from a transient one: the auth handler clears the
    /// stored tokens only when the server actually rejected them, so surviving tokens mean
    /// "offline/transient — enter the shell", while cleared tokens mean "session over — go to login".
    /// </summary>
    private async Task<bool> TryRehydrateSessionAsync()
    {
        try
        {
            await FetchIdentityAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Session resume failed: {ex}");
            return await _tokenStorage.GetAccessTokenAsync() is not null;
        }
    }

    private async Task FetchIdentityAsync()
    {
        var apiClient = Resolve<IApiClient>();
        var authState = Resolve<AuthStateService>();

        // SetUserProfile / SetEmployee persist the identity, so the next resume restores it instantly.
        var profile = await apiClient.GetMyProfileAsync();
        authState.SetUserProfile(new UserProfile(
            profile.Id, profile.Email, profile.FirstName, profile.LastName, profile.ImageUrl));

        var employee = await apiClient.GetMyEmployeeAsync();
        authState.SetEmployee(new EmployeeInfo(
            employee.EmployeeId, employee.FullName, employee.Department, employee.Position));
    }

    private T Resolve<T>() where T : notnull =>
        Handler!.MauiContext!.Services.GetRequiredService<T>();

    private T ResolvePage<T>() where T : Page => Resolve<T>();
}
