using Playground.Maui.Features.Asset;
using Playground.Maui.Features.Inventory;

namespace Playground.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
        Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
        Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
    }
}
