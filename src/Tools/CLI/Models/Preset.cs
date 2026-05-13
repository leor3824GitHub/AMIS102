namespace AMIS.CLI.Models;

internal sealed class Preset
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ProjectType Type { get; init; }
    public required ArchitectureStyle Architecture { get; init; }
    public required DatabaseProvider Database { get; init; }
    public required bool IncludeDocker { get; init; }
    public required bool IncludeAspire { get; init; }
    public required bool IncludeSampleModule { get; init; }
    public required bool IncludeTerraform { get; init; }
    public required bool IncludeGitHubActions { get; init; }

    public ProjectOptions ToProjectOptions(string projectName, string outputPath) => new()
    {
        Name = projectName,
        Type = Type,
        Architecture = Architecture,
        Database = Database,
        IncludeDocker = IncludeDocker,
        IncludeAspire = IncludeAspire,
        IncludeSampleModule = IncludeSampleModule,
        IncludeTerraform = IncludeTerraform,
        IncludeGitHubActions = IncludeGitHubActions,
        OutputPath = outputPath
    };
}

internal static class Presets
{
    public static Preset QuickStart { get; } = new()
    {
        Name = "Quick Start",
        Description = "API + Monolith + PostgreSQL + Docker + Sample Module",
        Type = ProjectType.Api,
        Architecture = ArchitectureStyle.Monolith,
        Database = DatabaseProvider.PostgreSQL,
        IncludeDocker = true,
        IncludeAspire = false,
        IncludeSampleModule = true,
        IncludeTerraform = false,
        IncludeGitHubActions = false
    };

    public static Preset ProductionReady { get; } = new()
    {
        Name = "Production Ready",
        Description = "API + Blazor + Monolith + PostgreSQL + Aspire + Terraform + CI",
        Type = ProjectType.ApiBlazor,
        Architecture = ArchitectureStyle.Monolith,
        Database = DatabaseProvider.PostgreSQL,
        IncludeDocker = true,
        IncludeAspire = true,
        IncludeSampleModule = false,
        IncludeTerraform = true,
        IncludeGitHubActions = true
    };

    public static Preset MicroservicesStarter { get; } = new()
    {
        Name = "Microservices Starter",
        Description = "API + Microservices + PostgreSQL + Docker + Aspire",
        Type = ProjectType.Api,
        Architecture = ArchitectureStyle.Microservices,
        Database = DatabaseProvider.PostgreSQL,
        IncludeDocker = true,
        IncludeAspire = true,
        IncludeSampleModule = false,
        IncludeTerraform = false,
        IncludeGitHubActions = false
    };

    public static Preset ServerlessApi { get; } = new()
    {
        Name = "Serverless API",
        Description = "API + Serverless (AWS Lambda) + PostgreSQL + Terraform",
        Type = ProjectType.Api,
        Architecture = ArchitectureStyle.Serverless,
        Database = DatabaseProvider.PostgreSQL,
        IncludeDocker = false,
        IncludeAspire = false,
        IncludeSampleModule = false,
        IncludeTerraform = true,
        IncludeGitHubActions = false
    };

    public static IReadOnlyList<Preset> All { get; } =
    [
        QuickStart,
        ProductionReady,
        MicroservicesStarter,
        ServerlessApi
    ];

    public static Preset? GetByName(string name) =>
        All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Replace(" ", string.Empty, StringComparison.Ordinal).Equals(name, StringComparison.OrdinalIgnoreCase));
}

