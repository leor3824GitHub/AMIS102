using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Playground.Maui.Data;
using Playground.Maui.Features.Asset;
using Playground.Maui.Features.Auth;
using Playground.Maui.Features.Inventory;
using Playground.Maui.Features.Profile;
using Playground.Maui.Features.Scan;
using Playground.Maui.Services;
using ZXing.Net.Maui.Controls;

namespace Playground.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts => { });

        // Configuration from embedded appsettings.json
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("Playground.Maui.appsettings.json");
        if (stream is not null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        var apiOptions = builder.Configuration
            .GetSection("Api")
            .Get<ApiClientOptions>() ?? new ApiClientOptions { BaseUrl = "http://localhost:5000" };

        // Services
        builder.Services.AddSingleton(apiOptions);
        builder.Services.AddSingleton<AuthStateService>();
        builder.Services.AddSingleton<LocalDb>();
        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
        builder.Services.AddTransient<AuthenticatedHttpHandler>();

        builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
            client.BaseAddress = new Uri(apiOptions.BaseUrl))
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<InventoryViewModel>();
        builder.Services.AddTransient<ICSDetailViewModel>();
        builder.Services.AddTransient<PARDetailViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<ICSDetailPage>();
        builder.Services.AddTransient<PARDetailPage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<AssetDetailPage>();

        return builder.Build();
    }
}
