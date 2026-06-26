using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using AMIS.Maui.Data;
using AMIS.Maui.Features.Asset;
using AMIS.Maui.Features.Auth;
using AMIS.Maui.Features.Chat;
using AMIS.Maui.Features.Home;
using AMIS.Maui.Features.Inventory;
using AMIS.Maui.Features.PhysicalCount;
using AMIS.Maui.Features.Profile;
using AMIS.Maui.Features.Scan;
using AMIS.Maui.Services;
using ZXing.Net.Maui.Controls;

namespace AMIS.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCamera()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        // Configuration: embedded appsettings.json (dev defaults), appsettings.Production.json
        // overlay in Release builds, then environment variables
        // (environment variables override so Aspire can inject Api__BaseUrl at launch time)
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("AMIS.Maui.appsettings.json");
        if (stream is not null)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonStream(stream);
#if !DEBUG
            using var productionStream = assembly.GetManifestResourceStream("AMIS.Maui.appsettings.Production.json");
            if (productionStream is not null)
            {
                configBuilder.AddJsonStream(productionStream);
            }
#endif
            var config = configBuilder
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

        // Restore the last-used tenant: App.OnStart resumes a session from stored tokens without
        // showing the login page, so the tenant header must match the tenant the tokens were issued under.
        apiOptions.TenantId = Preferences.Default.Get(ApiClientOptions.TenantPreferenceKey, apiOptions.TenantId);

        // Services
        builder.Services.AddSingleton(apiOptions);
        builder.Services.AddSingleton<AuthStateService>();
        builder.Services.AddSingleton<LocalDb>();
        builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
        builder.Services.AddSingleton<IOcrService, OcrService>();
        builder.Services.AddSingleton<ChatHubService>();
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
        builder.Services.AddTransient<HomeViewModel>();
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
        builder.Services.AddTransient<ChatChannelListViewModel>();
        builder.Services.AddTransient<ChatConversationViewModel>();

        // Pages
        builder.Services.AddTransient<HomePage>();
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
        builder.Services.AddTransient<ChatChannelListPage>();
        builder.Services.AddTransient<ChatConversationPage>();

        return builder.Build();
    }
}
