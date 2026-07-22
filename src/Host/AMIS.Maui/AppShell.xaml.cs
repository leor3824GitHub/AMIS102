using AMIS.Maui.Features.Asset;
using AMIS.Maui.Features.Chat;
using AMIS.Maui.Features.Home;
using AMIS.Maui.Features.Inventory;
using AMIS.Maui.Features.PhysicalCount;
using AMIS.Maui.Features.Profile;
using AMIS.Maui.Helpers;
using AMIS.Maui.Services;

namespace AMIS.Maui;

public partial class AppShell : Shell
{
    private readonly ChatUnreadService _unread;

    public AppShell(ChatUnreadService unread)
    {
        InitializeComponent();

        _unread = unread;
        _unread.Changed += OnUnreadChanged;

        // Connects the realtime hub for the whole session (not just while Chat is open) so the tab
        // lights up wherever the user is, then reconciles against the channel list once.
        _ = InitializeUnreadAsync();

        Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
        Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
        Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
        Routing.RegisterRoute(nameof(PhysicalCountScanPage), typeof(PhysicalCountScanPage));
        Routing.RegisterRoute(nameof(PhysicalCountEntriesPage), typeof(PhysicalCountEntriesPage));
        Routing.RegisterRoute(nameof(PhysicalCountChecklistPage), typeof(PhysicalCountChecklistPage));
        Routing.RegisterRoute(nameof(PhysicalCountFoundAtStationPage), typeof(PhysicalCountFoundAtStationPage));
        Routing.RegisterRoute(nameof(ChatConversationPage), typeof(ChatConversationPage));
        Routing.RegisterRoute(nameof(InventoryPage), typeof(InventoryPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
    }

    private async Task InitializeUnreadAsync()
    {
        try
        {
            await _unread.InitializeAsync();
        }
        catch (Exception ex)
        {
            // Fire-and-forget from a constructor: an escaping exception would land on the UI thread
            // with no handler and take the app down. Unread state is cosmetic — log and move on.
            System.Diagnostics.Debug.WriteLine($"[AppShell] unread init failed: {ex}");
        }
    }

    // Shell exposes no badge API, so the unread state rides the tab's icon: a chat bubble with a dot.
    private void OnUnreadChanged() => ChatTab.Icon = new FontImageSource
    {
        FontFamily = "MaterialSymbols",
        Glyph = _unread.HasUnread ? MaterialIcons.MarkChatUnread : MaterialIcons.ChatBubble,
        Size = 22,
    };

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
