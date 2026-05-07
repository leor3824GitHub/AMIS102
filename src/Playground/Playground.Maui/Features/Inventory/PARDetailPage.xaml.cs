using Microsoft.Extensions.DependencyInjection;

namespace Playground.Maui.Features.Inventory;

public partial class PARDetailPage : ContentPage
{
    public PARDetailPage()
        : this(ResolveViewModel())
    {
    }

    public PARDetailPage(PARDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static PARDetailViewModel ResolveViewModel() =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<PARDetailViewModel>()
        ?? throw new InvalidOperationException("Unable to resolve PARDetailViewModel from DI.");
}
