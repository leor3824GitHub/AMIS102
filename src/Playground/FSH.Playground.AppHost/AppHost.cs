var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("fsh-postgres-data")
    .AddDatabase("AMIS113");

var redis = builder.AddRedis("redis").WithDataVolume("fsh-redis-data");

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
    .WithEnvironment("CachingOptions__EnableSsl", "true")
    .WaitFor(redis);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor")
    .WithReference(api)
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

await builder.Build().RunAsync();
