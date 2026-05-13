using Microsoft.Extensions.DependencyInjection;
namespace AMIS.Framework.Blazor.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHeroUI(this IServiceCollection services)
    {
        services.AddMudServices(options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            options.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
        });

        services.AddMudPopoverService();
        services.AddScoped<AMIS.Framework.Blazor.UI.Components.Feedback.Snackbar.AMISSnackbar>();
        services.AddSingleton(AMIS.Framework.Blazor.UI.Theme.AMISTheme.Build());

        return services;
    }
}

