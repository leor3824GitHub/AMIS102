using AMIS.Maui.Features.Asset;
using AMIS.Maui.Features.Chat;
using AMIS.Maui.Features.Home;
using AMIS.Maui.Features.Inventory;
using AMIS.Maui.Features.PhysicalCount;

namespace AMIS.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
        Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
        Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
        Routing.RegisterRoute(nameof(PhysicalCountScanPage), typeof(PhysicalCountScanPage));
        Routing.RegisterRoute(nameof(PhysicalCountEntriesPage), typeof(PhysicalCountEntriesPage));
        Routing.RegisterRoute(nameof(PhysicalCountChecklistPage), typeof(PhysicalCountChecklistPage));
        Routing.RegisterRoute(nameof(PhysicalCountFoundAtStationPage), typeof(PhysicalCountFoundAtStationPage));
        Routing.RegisterRoute(nameof(ChatConversationPage), typeof(ChatConversationPage));
    }

    protected override bool OnBackButtonPressed()
    {
        // If a detail page is pushed on top of the current tab, let Shell pop it normally.
        var navStack = Current?.Navigation?.NavigationStack;
        if (navStack is not null && navStack.Count > 1)
        {
            return base.OnBackButtonPressed();
        }

        var location = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;

        // Already on the Home tab → allow default behavior (minimize / exit).
        if (location.EndsWith(nameof(HomePage), StringComparison.Ordinal))
        {
            return base.OnBackButtonPressed();
        }

        // On any other tab → switch to Home and consume the back press.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await GoToAsync($"//{nameof(HomePage)}");
            }
            catch
            {
                // Ignore navigation races on rapid back presses.
            }
        });
        return true;
    }
}
