namespace AMIS.CLI.Models;

internal sealed class ProjectOptions
{
    public required string Name { get; set; }
    public ProjectType Type { get; set; } = ProjectType.Api;
    public ArchitectureStyle Architecture { get; set; } = ArchitectureStyle.Monolith;
    public DatabaseProvider Database { get; set; } = DatabaseProvider.PostgreSQL;
    public bool IncludeDocker { get; set; } = true;
    public bool IncludeAspire { get; set; } = true;
    public bool IncludeSampleModule { get; set; }
    public bool IncludeTerraform { get; set; }
    public bool IncludeGitHubActions { get; set; }
    public bool InitializeGit { get; set; } = true;
    public string OutputPath { get; set; } = ".";

    /// <summary>
    /// Version of AMIS packages to use. If null, uses the CLI's version.
    /// </summary>
    public string? FrameworkVersion { get; set; }
}

internal enum ProjectType
{
    Api,
    ApiBlazor
}

internal enum ArchitectureStyle
{
    Monolith,
    Microservices,
    Serverless
}

internal enum DatabaseProvider
{
    PostgreSQL,
    SqlServer,
    SQLite
}

