using System.Globalization;
using AMIS.Framework.Blazor.UI;
using AMIS.Framework.Blazor.UI.Theme;
using AMIS.Blazor;
using AMIS.Blazor.ApiClient;
using AMIS.Blazor.Common;
using AMIS.Blazor.Components;
using AMIS.Blazor.Services;
using AMIS.Blazor.Services.AssetRegister;
using AMIS.Blazor.Services.Api;
using AMIS.Blazor.Services.Downloads;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

// Currency/number/date formatting flows from a single configured culture (default en-PH → ₱).
// Change "Localization:Culture" in appsettings.json to switch the app's currency everywhere.
var appCulture = builder.Configuration["Localization:Culture"] ?? "en-PH";
Money.Configure(appCulture);
try
{
    var ci = CultureInfo.GetCultureInfo(appCulture);
    CultureInfo.DefaultThreadCurrentCulture = ci;
    CultureInfo.DefaultThreadCurrentUICulture = ci;
}
catch (CultureNotFoundException)
{
    // Fall back to runtime default culture if the configured name is invalid.
}

// Configure HTTP/3 support (only override in production, respect launchSettings in dev)
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        });
    });
}

builder.Services.AddHeroUI();

// Authentication & Authorization
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Cookie Authentication for SSR support
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Security: Explicit cookie security settings
        options.Cookie.HttpOnly = true; // Prevent JavaScript access (XSS protection)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
        options.Cookie.Name = ".AMIS.Auth"; // Custom cookie name
    });

builder.Services.AddAuthorization();

// Distributed Cache (required by theme state factory).
// HA-ready: when a Redis connection string is configured (CachingOptions:Redis — same key the API
// uses), the distributed cache is backed by Redis so session/theme state is shared across UI nodes.
// With no Redis configured it falls back to in-memory, which is correct for a single UI node.
var distributedCacheRedis = builder.Configuration["CachingOptions:Redis"];
if (string.IsNullOrWhiteSpace(distributedCacheRedis))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var config = StackExchange.Redis.ConfigurationOptions.Parse(distributedCacheRedis);
        // UI tier stays available if Redis briefly drops — don't hard-fail the connection.
        config.AbortOnConnectFail = false;
        options.ConfigurationOptions = config;
    });
}

// Simple cookie-based authentication
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

// Tenant theme services
builder.Services.AddScoped<ITenantThemeState, TenantThemeState>(); // For Interactive mode
builder.Services.AddScoped<IThemeStateFactory, CachedThemeStateFactory>(); // For SSR mode

// PDF download hand-off — keeps generated PDF bytes off the SignalR circuit (see PdfDownloadCache)
builder.Services.AddSingleton<AMIS.Blazor.Services.Downloads.IPdfDownloadCache, AMIS.Blazor.Services.Downloads.PdfDownloadCache>();
builder.Services.AddScoped<AMIS.Blazor.Services.Downloads.IPdfDownloadService, AMIS.Blazor.Services.Downloads.PdfDownloadService>();

// User profile state for syncing across components
builder.Services.AddScoped<IUserProfileState, UserProfileState>();

// Organization profile state — loaded once per session in AMISLayout, consumed by report pages
builder.Services.AddScoped<IOrganizationProfileState, OrganizationProfileState>();

// Capitalization threshold state — loaded once per session, consumed by PR/receiving forms and reports
builder.Services.AddScoped<ICapitalizationThresholdState, CapitalizationThresholdState>();

// Renders the end-user guides (wwwroot/help/**.md) for the in-app Help pages
builder.Services.AddScoped<AMIS.Blazor.Services.Help.IHelpContentService, AMIS.Blazor.Services.Help.HelpContentService>();

// Bounded reference-data lookups — cached once per circuit; fixes the >100-row dropdown truncation.
builder.Services.AddScoped<ILookupDataService, LookupDataService>();

// Unpaged/batch inventory reads — warehouse pickers and per-product balances that the paged
// inventory search would silently truncate at the 100-row clamp.
builder.Services.AddScoped<IInventoryDataService, InventoryDataService>();

// Remembers each report's last-used Fast Report Page Setup in encrypted browser localStorage
builder.Services.AddScoped<IFastReportSetupPreferences, FastReportSetupPreferences>();

// Remembers each report's last-used QuestPDF Page Setup (paper, orientation, margin) per report
builder.Services.AddScoped<IQuestPdfSetupPreferences, QuestPdfSetupPreferences>();

// Remembers the user's last-used "Date Assumed Accountability" for the Physical Count report
builder.Services.AddScoped<IAssumedAccountabilityDatePreference, AssumedAccountabilityDatePreference>();

// Auth state notifier for session expiration (Blazor-compatible)
builder.Services.AddScoped<IAuthStateNotifier, AuthStateNotifier>();

// Session token store: the token pair lives server-side, keyed by the session id in the auth cookie.
// Critical: the API rotates the refresh token on every refresh and a circuit cannot rewrite the cookie
// (SignInAsync throws once the SignalR response has started), so tokens must not live in the cookie —
// otherwise a reload or second tab replays the rotated-away token and gets signed out.
builder.Services.AddSingleton<ISessionTokenStore, SessionTokenStore>();

// Circuit-scoped L1 over that store
builder.Services.AddScoped<ICircuitTokenCache, CircuitTokenCache>();

// Authorization header handler for API calls
builder.Services.AddScoped<AuthorizationHeaderHandler>();
builder.Services.AddScoped<ApiRetryHandler>();

// Token refresh service for handling expired access tokens
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();

// API health monitor for status indicator (component handles thread marshaling)
builder.Services.AddScoped<IApiHealthMonitor>(sp =>
{
    var healthClient = sp.GetRequiredService<IHealthClient>();
    var logger = sp.GetRequiredService<ILogger<ApiHealthMonitor>>();
    return new ApiHealthMonitor(healthClient, logger);
});

builder.Services.AddHttpClient();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                 ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");

var apiUri = new Uri(apiBaseUrl);

builder.Services.AddHttpClient("ThemeClient", client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    if (builder.Environment.IsDevelopment() &&
        (string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    {
#pragma warning disable S4830
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830
    }

    return handler;
});

// Configure HttpClient with authorization handler for API calls
builder.Services.AddScoped(sp =>
{
    var retryHandler = sp.GetRequiredService<ApiRetryHandler>();
    var handler = sp.GetRequiredService<AuthorizationHeaderHandler>();
    var innerHandler = new HttpClientHandler();

    if (builder.Environment.IsDevelopment() &&
        (string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    {
#pragma warning disable S4830
        innerHandler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830
    }

    handler.InnerHandler = innerHandler;
    retryHandler.InnerHandler = handler;

    return new HttpClient(retryHandler)
    {
        BaseAddress = apiUri,
        Timeout = TimeSpan.FromSeconds(30) // Reduced from default 100s to speed up failures
    };
});

builder.Services.AddApiClients(builder.Configuration, builder.Environment);

// Circuit-scoped SignalR client for the app-wide chat/realtime hub (one connection per circuit).
builder.Services.AddScoped<AMIS.Blazor.Services.Chat.ChatHubClient>();

// Circuit-scoped notification bell state (unread count + recent), fed by REST load + live hub pushes.
builder.Services.AddScoped<AMIS.Blazor.Services.Notifications.INotificationState, AMIS.Blazor.Services.Notifications.NotificationState>();

// Register MasterData service for Supplier and Category operations
builder.Services.AddScoped<MasterDataService>();
builder.Services.AddScoped<IAssetRegisterReportsClient, AssetRegisterReportsClient>();

// Response Compression for static assets and API responses
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Output Caching for static responses
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
#pragma warning disable CA1307 // PathString.StartsWithSegments is case-insensitive by design
        .With(c => c.HttpContext.Request.Path.StartsWithSegments("/health"))
#pragma warning restore CA1307
        .Expire(TimeSpan.FromSeconds(10)));
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment())
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 512 * 1024); // 512 KB — default 32 KB is too small for large form posts/uploads over the circuit

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Simple health endpoints for ALB/ECS
app.MapGet("/health/ready", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }))
   .AllowAnonymous();

app.UseResponseCompression(); // Must come before UseStaticFiles
app.UseOutputCache();
app.UseHttpsRedirection();
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();
app.UseAntiforgery();

app.MapSimpleBffAuthEndpoints();
app.MapPdfDownloadEndpoints();
app.MapAssetImageEndpoints();
app.MapProductImageEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

