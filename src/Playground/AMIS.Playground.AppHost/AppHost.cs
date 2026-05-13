var builder = DistributedApplication.CreateBuilder(args);

// Fixed API ports + LAN IP so a physical Android phone can reach the dev API over Wi-Fi.
// Override LAN IP via env var MAUI_ANDROID_API_HOST when running on a different machine.
const int ApiHttpPort = 5030;
const int ApiHttpsPort = 7030;
var lanHost = Environment.GetEnvironmentVariable("MAUI_ANDROID_API_HOST") ?? "10.0.254.4";
var androidApiBaseUrl = $"https://{lanHost}:{ApiHttpsPort}";
var localApiBaseUrl = $"https://localhost:{ApiHttpsPort}";

// Postgres container + database
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("AMIS-postgres-data229")
    .AddDatabase("AMIS229");

var redis = builder.AddRedis("redis", port: 6379)
    .WithDataVolume("AMIS-redis-data229");

var api = builder.AddProject<Projects.Playground_Api>("playground-api")
    // Modify the endpoints auto-created from launchSettings.json (http/https profiles).
    // isProxied:false → dashboard shows the real port (5030/7030), no Aspire reverse proxy.
    .WithEndpoint("http", e => { e.Port = ApiHttpPort; e.IsProxied = false; })
    .WithEndpoint("https", e => { e.Port = ApiHttpsPort; e.IsProxied = false; })
    .WithReference(postgres)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    // Override binding to all interfaces so the LAN-connected phone can reach Kestrel.
    // (WithHttpEndpoint/WithHttpsEndpoint bind to localhost by default — too restrictive for device testing.)
    .WithEnvironment("ASPNETCORE_URLS", $"http://*:{ApiHttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture)};https://*:{ApiHttpsPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "AMIS.Playground.Migrations.PostgreSQL")
    .WithEnvironment("MultitenancyOptions__UseDistributedCacheStore", "true")
    .WaitFor(postgres)
    .WithReference(redis)
    .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
    .WaitFor(redis);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor")
    .WithReference(api)
    .WithEnvironment("Api__BaseUrl", localApiBaseUrl)
    .WaitFor(api);

builder.AddExecutable(
        "playground-maui",
        "dotnet",
        Path.Combine("..", "Playground.Maui"),
        "run", "--framework", "net10.0-windows10.0.19041.0")
    .WithEnvironment("Api__BaseUrl", localApiBaseUrl)
    .WaitFor(api);

// Android: requires an emulator or physical device connected via ADB.
// Uses LAN IP so a physical phone on the same Wi-Fi can reach the dev API.
// Debug builds bypass cert validation on Android since the dev cert isn't trusted there.
builder.AddExecutable(
        "playground-maui-android",
        "dotnet",
        Path.Combine("..", "Playground.Maui"),
        "run", "--framework", "net10.0-android")
    .WithEnvironment("Api__BaseUrl", androidApiBaseUrl)
    .WaitFor(api);

await builder.Build().RunAsync();

