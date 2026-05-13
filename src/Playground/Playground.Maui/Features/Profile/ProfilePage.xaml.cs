using Microsoft.Extensions.DependencyInjection;

namespace Playground.Maui.Features.Profile;

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
        await _vm.LoadAsync();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _vm.LogoutAsync();
    }

    private static ProfileViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ProfileViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ProfileViewModel from DI.");
}
