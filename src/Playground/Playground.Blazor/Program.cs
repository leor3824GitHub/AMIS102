using AMIS.Framework.Blazor.UI;
using AMIS.Framework.Blazor.UI.Theme;
using AMIS.Playground.Blazor;
using AMIS.Playground.Blazor.ApiClient;
using AMIS.Playground.Blazor.Components;
using AMIS.Playground.Blazor.Services;
using AMIS.Playground.Blazor.Services.AssetRegister;
using AMIS.Playground.Blazor.Services.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

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
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // Security: Explicit cookie security settings
        options.Cookie.HttpOnly = true; // Prevent JavaScript access (XSS protection)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
        options.Cookie.Name = ".AMIS.Auth"; // Custom cookie name
    });

builder.Services.AddAuthorization();

// Distributed Cache (required by theme state factory)
builder.Services.AddDistributedMemoryCache();

// Simple cookie-based authentication
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

// Tenant theme services
builder.Services.AddScoped<ITenantThemeState, TenantThemeState>(); // For Interactive mode
builder.Services.AddScoped<IThemeStateFactory, CachedThemeStateFactory>(); // For SSR mode

// User profile state for syncing across components
builder.Services.AddScoped<IUserProfileState, UserProfileState>();

// Organization profile state — loaded once per session in PlaygroundLayout, consumed by report pages
builder.Services.AddScoped<IOrganizationProfileState, OrganizationProfileState>();

// Auth state notifier for session expiration (Blazor-compatible)
builder.Services.AddScoped<IAuthStateNotifier, AuthStateNotifier>();

// Circuit-scoped token cache for storing refreshed tokens
// Critical: httpContext.User claims are cached per circuit and don't update after SignInAsync
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
    .AddInteractiveServerComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment());

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
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

