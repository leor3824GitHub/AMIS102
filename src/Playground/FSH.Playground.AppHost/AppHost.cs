var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("fsh-postgres-data226")
    .AddDatabase("AMIS226");

var redis = builder.AddRedis("redis", port: 6379)
    .WithDataVolume("fsh-redis-data226");

var api = builder.AddProject<Projects.Playground_Api>("playground-api")
    .WithReference(postgres)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Playground.Migrations.PostgreSQL")
    .WithEnvironment("MultitenancyOptions__UseDistributedCacheStore", "true")
    .WaitFor(postgres)
    .WithReference(redis)
    .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
    .WaitFor(redis);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor")
    .WithReference(api)
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

builder.AddExecutable(
        "playground-maui",
        "dotnet",
        Path.Combine("..", "Playground.Maui"),
        "run", "--framework", "net10.0-windows10.0.19041.0")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

await builder.Build().RunAsync();
