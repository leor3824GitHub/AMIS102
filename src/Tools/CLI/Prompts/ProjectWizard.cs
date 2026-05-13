using System.Reflection;
using AMIS.CLI.Models;
using AMIS.CLI.UI;
using AMIS.CLI.Validation;
using Spectre.Console;

namespace AMIS.CLI.Prompts;

internal static class ProjectWizard
{
    public static ProjectOptions Run(string? initialName = null, string? initialVersion = null)
    {
        ConsoleTheme.WriteBanner();

        // Step 1: Choose preset or custom
        var startChoice = PromptStartChoice();

        if (startChoice != "Custom")
        {
            var preset = Presets.All.First(p => p.Name == startChoice);
            var presetName = PromptProjectName(initialName);
            var presetPath = PromptOutputPath();
            var presetVersion = PromptFrameworkVersion(initialVersion);

            var presetOptions = preset.ToProjectOptions(presetName, presetPath);
            presetOptions.FrameworkVersion = presetVersion;

            ShowSummary(presetOptions);
            return presetOptions;
        }

        // Custom flow
        var name = PromptProjectName(initialName);
        var type = PromptProjectType();
        var architecture = PromptArchitecture(type);
        var database = PromptDatabase(architecture);
        var features = PromptFeatures(architecture);
        var outputPath = PromptOutputPath();
        var frameworkVersion = PromptFrameworkVersion(initialVersion);

        var options = new ProjectOptions
        {
            Name = name,
            Type = type,
            Architecture = architecture,
            Database = database,
            InitializeGit = features.Contains("Git Repository"),
            IncludeDocker = features.Contains("Docker Compose"),
            IncludeAspire = features.Contains("Aspire AppHost"),
            IncludeSampleModule = features.Contains("Sample Module (Catalog)"),
            IncludeTerraform = features.Contains("Terraform (AWS)"),
            IncludeGitHubActions = features.Contains("GitHub Actions CI"),
            OutputPath = outputPath,
            FrameworkVersion = frameworkVersion
        };

        ShowSummary(options);
        return options;
    }

    private static string PromptStartChoice()
    {
        var choices = new List<string> { "Custom" };
        choices.AddRange(Presets.All.Select(p => p.Name));

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Select template[/]")
                .PageSize(10)
                .HighlightStyle(ConsoleTheme.PrimaryStyle)
                .AddChoices(choices)
                .UseConverter(c =>
                {
                    if (c == "Custom")
                        return "Custom [dim]- configure manually[/]";

                    var preset = Presets.All.First(p => p.Name == c);
                    return $"{preset.Name} [dim]- {preset.Description}[/]";
                }));

        return choice;
    }

    private static string PromptProjectName(string? initialName)
    {
        if (!string.IsNullOrWhiteSpace(initialName) && OptionValidator.IsValidProjectName(initialName))
        {
            return initialName;
        }

        return AnsiConsole.Prompt(
            new TextPrompt<string>("[dim]Project name:[/]")
                .PromptStyle(ConsoleTheme.PrimaryStyle)
                .ValidationErrorMessage("[red]Invalid name[/]")
                .Validate(name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return Spectre.Console.ValidationResult.Error("Required");

                    if (!char.IsLetter(name[0]))
                        return Spectre.Console.ValidationResult.Error("Must start with a letter");

                    if (!name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
                        return Spectre.Console.ValidationResult.Error("Only letters, numbers, _, -, or .");

                    return Spectre.Console.ValidationResult.Success();
                }));
    }

    private static ProjectType PromptProjectType()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Project type[/]")
                .HighlightStyle(ConsoleTheme.PrimaryStyle)
                .AddChoices("API", "API + Blazor"));

        return choice == "API" ? ProjectType.Api : ProjectType.ApiBlazor;
    }

    private static ArchitectureStyle PromptArchitecture(ProjectType projectType)
    {
        var choices = new List<string>
        {
            "Monolith",
            "Microservices"
        };

        // Serverless not available with Blazor
        if (projectType == ProjectType.Api)
        {
            choices.Add("Serverless");
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Architecture[/]")
                .HighlightStyle(ConsoleTheme.PrimaryStyle)
                .AddChoices(choices));

        return choice switch
        {
            "Monolith" => ArchitectureStyle.Monolith,
            "Microservices" => ArchitectureStyle.Microservices,
            "Serverless" => ArchitectureStyle.Serverless,
            _ => ArchitectureStyle.Monolith
        };
    }

    private static DatabaseProvider PromptDatabase(ArchitectureStyle architecture)
    {
        var choices = new List<string>
        {
            "PostgreSQL",
            "SQL Server"
        };

        // SQLite not available with Microservices
        if (architecture != ArchitectureStyle.Microservices)
        {
            choices.Add("SQLite");
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Database[/]")
                .HighlightStyle(ConsoleTheme.PrimaryStyle)
                .AddChoices(choices));

        return choice switch
        {
            "PostgreSQL" => DatabaseProvider.PostgreSQL,
            "SQL Server" => DatabaseProvider.SqlServer,
            "SQLite" => DatabaseProvider.SQLite,
            _ => DatabaseProvider.PostgreSQL
        };
    }

    private static List<string> PromptFeatures(ArchitectureStyle architecture)
    {
        var choices = new List<string>
        {
            "Git Repository",
            "Docker Compose",
            "Sample Module (Catalog)",
            "Terraform (AWS)",
            "GitHub Actions CI"
        };

        // Aspire not available with Serverless
        if (architecture != ArchitectureStyle.Serverless)
        {
            choices.Insert(2, "Aspire AppHost");
        }

        var defaults = new List<string> { "Git Repository", "Docker Compose" };
        if (architecture != ArchitectureStyle.Serverless)
        {
            defaults.Add("Aspire AppHost");
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title("[dim]Features[/] [dim italic](space to toggle)[/]")
            .HighlightStyle(ConsoleTheme.PrimaryStyle)
            .InstructionsText("")
            .AddChoices(choices);

        foreach (var item in defaults)
        {
            prompt.Select(item);
        }

        return AnsiConsole.Prompt(prompt);
    }

    private static string PromptOutputPath()
    {
        var useCurrentDir = AnsiConsole.Confirm("[dim]Create in current directory?[/]", true);

        if (useCurrentDir)
        {
            return ".";
        }

        return AnsiConsole.Prompt(
            new TextPrompt<string>("[dim]Output path:[/]")
                .PromptStyle(ConsoleTheme.PrimaryStyle)
                .DefaultValue(".")
                .ValidationErrorMessage("[red]Invalid path[/]")
                .Validate(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return Spectre.Console.ValidationResult.Error("Required");

                    return Spectre.Console.ValidationResult.Success();
                }));
    }

    private static string? PromptFrameworkVersion(string? initialVersion)
    {
        // If a version was provided via CLI, use it
        if (!string.IsNullOrWhiteSpace(initialVersion))
        {
            return initialVersion;
        }

        var defaultVersion = GetDefaultFrameworkVersion();

        var useDefault = AnsiConsole.Confirm(
            $"[dim]Use default AMIS version[/] [cyan]{defaultVersion}[/][dim]?[/]",
            true);

        if (useDefault)
        {
            return null; // null means use CLI's version
        }

        return AnsiConsole.Prompt(
            new TextPrompt<string>("[dim]AMIS version:[/]")
                .PromptStyle(ConsoleTheme.PrimaryStyle)
                .DefaultValue(defaultVersion)
                .ValidationErrorMessage("[red]Invalid version[/]")
                .Validate(version =>
                {
                    if (string.IsNullOrWhiteSpace(version))
                        return Spectre.Console.ValidationResult.Error("Required");

                    // Basic semver validation
                    if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-[\w\d\.]+)?$"))
                        return Spectre.Console.ValidationResult.Error("Use semver format (e.g., 10.0.0)");

                    return Spectre.Console.ValidationResult.Success();
                }));
    }

    private static string GetDefaultFrameworkVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "10.0.0";

        // Remove any +buildmetadata suffix
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? version[..plusIndex] : version;
    }

    private static void ShowSummary(ProjectOptions options)
    {
        ConsoleTheme.WriteHeader("Configuration");

        ConsoleTheme.WriteKeyValue("Name", options.Name, highlight: true);
        ConsoleTheme.WriteKeyValue("Type", FormatEnum(options.Type));
        ConsoleTheme.WriteKeyValue("Architecture", FormatEnum(options.Architecture));
        ConsoleTheme.WriteKeyValue("Database", FormatEnum(options.Database));
        ConsoleTheme.WriteKeyValue("Version", options.FrameworkVersion ?? GetDefaultFrameworkVersion());
        ConsoleTheme.WriteKeyValue("Output", options.OutputPath);

        // Build features list
        var features = new List<string>();
        if (options.InitializeGit) features.Add("Git");
        if (options.IncludeDocker) features.Add("Docker");
        if (options.IncludeAspire) features.Add("Aspire");
        if (options.IncludeSampleModule) features.Add("Sample");
        if (options.IncludeTerraform) features.Add("Terraform");
        if (options.IncludeGitHubActions) features.Add("CI");

        ConsoleTheme.WriteKeyValue("Features", features.Count > 0 ? string.Join(", ", features) : "none");

        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Create project?", true))
        {
            AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
            Environment.Exit(0);
        }
    }

    private static string FormatEnum<T>(T value) where T : Enum =>
        value.ToString() switch
        {
            "Api" => "API",
            "ApiBlazor" => "API + Blazor",
            "PostgreSQL" => "PostgreSQL",
            "SqlServer" => "SQL Server",
            "SQLite" => "SQLite",
            _ => value.ToString()
        };
}

