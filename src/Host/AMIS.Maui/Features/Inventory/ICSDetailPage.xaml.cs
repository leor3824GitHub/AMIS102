using Microsoft.Extensions.DependencyInjection;

namespace AMIS.Maui.Features.Inventory;

public partial class ICSDetailPage : ContentPage
{
    public ICSDetailPage()
        : this(ResolveViewModel())
    {
    }

    public ICSDetailPage(ICSDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static ICSDetailViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ICSDetailViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve ICSDetailViewModel from DI.");
}
