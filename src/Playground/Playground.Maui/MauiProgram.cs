using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Playground.Maui.Data;
using Playground.Maui.Features.Asset;
using Playground.Maui.Features.Auth;
using Playground.Maui.Features.Inventory;
using Playground.Maui.Features.PhysicalCount;
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

        // Configuration: embedded appsettings.json, then environment variables
        // (environment variables override so Aspire can inject Api__BaseUrl at launch time)
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("Playground.Maui.appsettings.json");
        if (stream is not null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .AddEnvironmentVariables()
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        var apiOptions = builder.Configuration
            .GetSection("Api")
            .Get<ApiClientOptions>() ?? new ApiClientOptions { BaseUrl = "http://localhost:5030" };

        if (OperatingSystem.IsAndroid())
        {
            // Android emulator cannot reach host loopback via localhost; use 10.0.2.2.
            if (Uri.TryCreate(apiOptions.BaseUrl, UriKind.Absolute, out var uri) &&
                (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host == "127.0.0.1"))
            {
                var uriBuilder = new UriBuilder(uri)
                {
                    Host = "10.0.2.2"
                };

                apiOptions.BaseUrl = uriBuilder.Uri.ToString().TrimEnd('/');
            }
        }

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

        builder.Services.AddTransient<IPhysicalCountSyncService, PhysicalCountSyncService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<InventoryViewModel>();
        builder.Services.AddTransient<ICSDetailViewModel>();
        builder.Services.AddTransient<PARDetailViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();
        builder.Services.AddTransient<PhysicalCountSessionListViewModel>();
        builder.Services.AddTransient<PhysicalCountWalkthroughViewModel>();
        builder.Services.AddTransient<PhysicalCountMarkEntryViewModel>();
        builder.Services.AddTransient<PhysicalCountFoundAtStationViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<ICSDetailPage>();
        builder.Services.AddTransient<PARDetailPage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<AssetDetailPage>();
        builder.Services.AddTransient<PhysicalCountSessionListPage>();
        builder.Services.AddTransient<PhysicalCountWalkthroughPage>();
        builder.Services.AddTransient<PhysicalCountMarkEntryPage>();
        builder.Services.AddTransient<PhysicalCountFoundAtStationPage>();

        return builder.Build();
    }
}
