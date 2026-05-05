namespace Playground.Maui.Features.Asset;

public partial class AssetDetailPage : ContentPage
{
    public AssetDetailPage(AssetDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
