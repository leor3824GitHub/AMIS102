using Microsoft.Extensions.DependencyInjection;

namespace Playground.Maui.Features.Asset;

public partial class AssetDetailPage : ContentPage
{
    public AssetDetailPage()
        : this(ResolveViewModel())
    {
    }

    public AssetDetailPage(AssetDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static AssetDetailViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<AssetDetailViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve AssetDetailViewModel from DI.");
}
