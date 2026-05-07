using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FSH.CLI.Models;
using FSH.CLI.Prompts;
using FSH.CLI.Scaffolding;
using FSH.CLI.UI;
using FSH.CLI.Validation;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
internal sealed class NewCommand : AsyncCommand<NewCommand.Settings>
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli via reflection")]
    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        [Description("The name of the project")]
        public string? Name { get; set; }

        [CommandOption("-t|--type")]
        [Description("Project type: api, api-blazor")]
        [DefaultValue(null)]
        public string? Type { get; set; }

        [CommandOption("-a|--arch")]
        [Description("Architecture style: monolith, microservices, serverless")]
        [DefaultValue(null)]
        public string? Architecture { get; set; }

        [CommandOption("-d|--db")]
        [Description("Database provider: postgres, sqlserver, sqlite")]
        [DefaultValue(null)]
        public string? Database { get; set; }

        [CommandOption("-p|--preset")]
        [Description("Use a preset: quickstart, production, microservices, serverless")]
        [DefaultValue(null)]
        public string? Preset { get; set; }

        [CommandOption("-o|--output")]
        [Description("Output directory")]
        [DefaultValue(".")]
        public string Output { get; set; } = ".";

        [CommandOption("--docker")]
        [Description("Include Docker Compose")]
        [DefaultValue(null)]
        public bool? Docker { get; set; }

        [CommandOption("--aspire")]
        [Description("Include Aspire AppHost")]
        [DefaultValue(null)]
        public bool? Aspire { get; set; }

        [CommandOption("--sample")]
        [Description("Include sample module")]
        [DefaultValue(null)]
        public bool? Sample { get; set; }

        [CommandOption("--terraform")]
        [Description("Include Terraform (AWS)")]
        [DefaultValue(null)]
        public bool? Terraform { get; set; }

        [CommandOption("--ci")]
        [Description("Include GitHub Actions CI")]
        [DefaultValue(null)]
        public bool? CI { get; set; }

        [CommandOption("--git")]
        [Description("Initialize git repository")]
        [DefaultValue(null)]
        public bool? Git { get; set; }

        [CommandOption("-v|--fsh-version")]
        [Description("FullStackHero package version (e.g., 10.0.0 or 10.0.0-rc.1)")]
        [DefaultValue(null)]
        public string? FshVersion { get; set; }

        [CommandOption("--no-interactive")]
        [Description("Disable interactive mode")]
        [DefaultValue(false)]
        public bool NoInteractive { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            ProjectOptions options;

            if (settings.NoInteractive || HasExplicitOptions(settings))
            {
                options = BuildOptionsFromSettings(settings);

                var validation = OptionValidator.Validate(options);
                if (!validation.IsValid)
                {
                    foreach (var error in validation.Errors)
                    {
                        ConsoleTheme.WriteError(error);
                    }
                    return 1;
                }
            }
            else
            {
                options = ProjectWizard.Run(settings.Name, settings.FshVersion);
            }

            await SolutionGenerator.GenerateAsync(options, cancellationToken);

            return 0;
        }
        catch (ArgumentException ex)
        {
            ConsoleTheme.WriteError(ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleTheme.WriteError(ex.Message);
            return 1;
        }
        catch (IOException ex)
        {
            ConsoleTheme.WriteError($"File operation failed: {ex.Message}");
            return 1;
        }
    }

    private static bool HasExplicitOptions(Settings settings) =>
        !string.IsNullOrEmpty(settings.Preset) ||
        !string.IsNullOrEmpty(settings.Type) ||
        !string.IsNullOrEmpty(settings.Architecture) ||
        !string.IsNullOrEmpty(settings.Database);

    private static ProjectOptions BuildOptionsFromSettings(Settings settings)
    {
        // If preset is specified, use it as base
        if (!string.IsNullOrEmpty(settings.Preset))
        {
            var preset = settings.Preset.ToUpperInvariant() switch
            {
                "QUICKSTART" or "QUICK" => Presets.QuickStart,
                "PRODUCTION" or "PROD" => Presets.ProductionReady,
                "MICROSERVICES" or "MICRO" => Presets.MicroservicesStarter,
                "SERVERLESS" or "LAMBDA" => Presets.ServerlessApi,
                _ => throw new ArgumentException($"Unknown preset: {settings.Preset}")
            };

            var name = settings.Name ?? throw new ArgumentException("Project name is required");
            var options = preset.ToProjectOptions(name, settings.Output);

            // Allow overrides
            if (settings.Docker.HasValue) options.IncludeDocker = settings.Docker.Value;
            if (settings.Aspire.HasValue) options.IncludeAspire = settings.Aspire.Value;
            if (settings.Sample.HasValue) options.IncludeSampleModule = settings.Sample.Value;
            if (settings.Terraform.HasValue) options.IncludeTerraform = settings.Terraform.Value;
            if (settings.CI.HasValue) options.IncludeGitHubActions = settings.CI.Value;
            if (settings.Git.HasValue) options.InitializeGit = settings.Git.Value;
            if (!string.IsNullOrEmpty(settings.FshVersion)) options.FrameworkVersion = settings.FshVersion;

            return options;
        }

        // Build from individual options
        var projectName = settings.Name ?? throw new ArgumentException("Project name is required in non-interactive mode");

        return new ProjectOptions
        {
            Name = projectName,
            OutputPath = settings.Output,
            Type = ParseProjectType(settings.Type),
            Architecture = ParseArchitecture(settings.Architecture),
            Database = ParseDatabase(settings.Database),
            InitializeGit = settings.Git ?? true,
            IncludeDocker = settings.Docker ?? true,
            IncludeAspire = settings.Aspire ?? true,
            IncludeSampleModule = settings.Sample ?? false,
            IncludeTerraform = settings.Terraform ?? false,
            IncludeGitHubActions = settings.CI ?? false,
            FrameworkVersion = settings.FshVersion
        };
    }

    private static ProjectType ParseProjectType(string? type) =>
        type?.ToUpperInvariant() switch
        {
            "API" => ProjectType.Api,
            "API-BLAZOR" or "APIBLAZOR" or "BLAZOR" or "FULLSTACK" => ProjectType.ApiBlazor,
            null => ProjectType.Api,
            _ => throw new ArgumentException($"Unknown project type: {type}")
        };

    private static ArchitectureStyle ParseArchitecture(string? arch) =>
        arch?.ToUpperInvariant() switch
        {
            "MONOLITH" or "MONO" => ArchitectureStyle.Monolith,
            "MICROSERVICES" or "MICRO" => ArchitectureStyle.Microservices,
            "SERVERLESS" or "LAMBDA" => ArchitectureStyle.Serverless,
            null => ArchitectureStyle.Monolith,
            _ => throw new ArgumentException($"Unknown architecture: {arch}")
        };

    private static DatabaseProvider ParseDatabase(string? db) =>
        db?.ToUpperInvariant() switch
        {
            "POSTGRES" or "POSTGRESQL" or "PG" => DatabaseProvider.PostgreSQL,
            "SQLSERVER" or "MSSQL" or "SQL" => DatabaseProvider.SqlServer,
            "SQLITE" => DatabaseProvider.SQLite,
            null => DatabaseProvider.PostgreSQL,
            _ => throw new ArgumentException($"Unknown database provider: {db}")
        };
}
