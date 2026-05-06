using Playground.Maui.Features.Asset;
using Playground.Maui.Features.Inventory;
using Playground.Maui.Features.PhysicalCount;

namespace Playground.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
        Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
        Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
        Routing.RegisterRoute(nameof(PhysicalCountWalkthroughPage), typeof(PhysicalCountWalkthroughPage));
        Routing.RegisterRoute(nameof(PhysicalCountMarkEntryPage), typeof(PhysicalCountMarkEntryPage));
        Routing.RegisterRoute(nameof(PhysicalCountFoundAtStationPage), typeof(PhysicalCountFoundAtStationPage));
    }
}
