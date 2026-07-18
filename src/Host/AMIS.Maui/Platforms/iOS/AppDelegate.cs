using Foundation;

namespace AMIS.Maui;

// CA1711: the "AppDelegate" name is required by iOS — it is the [Register] symbol UIApplication.Main
// looks up at launch, so renaming it would break app startup.
#pragma warning disable CA1711
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
#pragma warning restore CA1711
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
