using AMIS.Maui.Features.Asset;
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
        Routing.RegisterRoute(nameof(PhysicalCountWalkthroughPage), typeof(PhysicalCountWalkthroughPage));
        Routing.RegisterRoute(nameof(PhysicalCountScanPage), typeof(PhysicalCountScanPage));
        Routing.RegisterRoute(nameof(PhysicalCountMarkEntryPage), typeof(PhysicalCountMarkEntryPage));
        Routing.RegisterRoute(nameof(PhysicalCountFoundAtStationPage), typeof(PhysicalCountFoundAtStationPage));
    }
}
