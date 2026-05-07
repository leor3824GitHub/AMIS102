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
            // Android cannot reach host loopback via localhost. Physical devices need the host's
            // LAN IP (Api:AndroidHost); emulators fall back to 10.0.2.2 when AndroidHost is empty.
            if (Uri.TryCreate(apiOptions.BaseUrl, UriKind.Absolute, out var uri) &&
                (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host == "127.0.0.1"))
            {
                var androidHost = string.IsNullOrWhiteSpace(apiOptions.AndroidHost)
                    ? "10.0.2.2"
                    : apiOptions.AndroidHost;

                var uriBuilder = new UriBuilder(uri)
                {
                    Host = androidHost
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
        builder.Services.AddSingleton<IOcrService, OcrService>();
        builder.Services.AddTransient<AuthenticatedHttpHandler>();

        var apiClientBuilder = builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
            client.BaseAddress = new Uri(apiOptions.BaseUrl))
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();

#if DEBUG && ANDROID
        // Android emulator does not trust the .NET HTTPS dev cert. Allow self-signed only in Debug.
        apiClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });
#endif

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
        builder.Services.AddTransient<PhysicalCountScanViewModel>();
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
        builder.Services.AddTransient<PhysicalCountScanPage>();
        builder.Services.AddTransient<PhysicalCountMarkEntryPage>();
        builder.Services.AddTransient<PhysicalCountFoundAtStationPage>();

        return builder.Build();
    }
}
