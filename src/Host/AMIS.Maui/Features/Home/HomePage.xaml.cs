using Microsoft.Extensions.DependencyInjection;

namespace AMIS.Maui.Features.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage()
        : this(ResolveViewModel())
    {
    }

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Because this override is async void, any exception that escapes is rethrown on the UI
        // thread and hard-crashes the app before a window is ever shown on Windows. LoadAsync
        // already handles the expected network failures, so this guard covers everything else.
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomePage] Dashboard load failed: {ex}");
            _vm.ErrorMessage = "Could not load your dashboard. Pull down to retry.";
        }
    }

    private void OnRecentTapped(object sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: RecentItem item } &&
            _vm.OpenRecentCommand.CanExecute(item))
        {
            _vm.OpenRecentCommand.Execute(item);
        }
    }

    private static HomeViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<HomeViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve HomeViewModel from DI.");
}
