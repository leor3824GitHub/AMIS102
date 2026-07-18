using Microsoft.Extensions.DependencyInjection;

namespace AMIS.Maui.Features.Profile;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm;

    public ProfilePage()
        : this(ResolveViewModel())
    {
    }

    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OnAppearing is async void: any exception that escapes here is posted to the UI
        // SynchronizationContext with no handler and hard-crashes the app. LoadAsync already
        // handles its own failures; this catch is a last-resort guard so a page appearance can
        // never take the process down.
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] OnAppearing failed: {ex}");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var pending = await _vm.GetPendingSyncCountAsync();

        var entryNoun = pending == 1 ? "entry" : "entries";
        var message = pending > 0
            ? $"You have {pending} count {entryNoun} that haven't synced yet. " +
              "Signing out will permanently discard them. Sign out anyway?"
            : "You'll need to log in again, and cached inventory will be cleared. Sign out now?";

        var confirmed = await DisplayAlertAsync("Sign Out", message, "Sign Out", "Cancel");

        if (confirmed)
            await _vm.LogoutAsync();
    }

    private static ProfileViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ProfileViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ProfileViewModel from DI.");
}
