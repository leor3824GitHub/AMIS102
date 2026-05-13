using AMIS.CLI.Models;

namespace AMIS.CLI.Validation;

internal static class OptionValidator
{
    public static OptionValidationResult Validate(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Serverless + Blazor is not supported
        if (options.Architecture == ArchitectureStyle.Serverless && options.Type == ProjectType.ApiBlazor)
        {
            errors.Add("Serverless architecture does not support Blazor. Please choose API only.");
        }

        // Microservices + SQLite is not supported
        if (options.Architecture == ArchitectureStyle.Microservices && options.Database == DatabaseProvider.SQLite)
        {
            errors.Add("Microservices architecture does not support SQLite. Please choose PostgreSQL or SQL Server.");
        }

        // Serverless typically doesn't use Aspire
        if (options.Architecture == ArchitectureStyle.Serverless && options.IncludeAspire)
        {
            errors.Add("Serverless architecture does not support Aspire AppHost.");
        }

        // Project name validation
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            errors.Add("Project name is required.");
        }
        else if (!IsValidProjectName(options.Name))
        {
            errors.Add("Project name must start with a letter and contain only letters, numbers, underscores, or hyphens.");
        }

        return errors.Count == 0
            ? OptionValidationResult.Success()
            : OptionValidationResult.Failure(errors);
    }

    public static bool IsValidCombination(ArchitectureStyle architecture, ProjectType type) =>
        !(architecture == ArchitectureStyle.Serverless && type == ProjectType.ApiBlazor);

    public static bool IsValidCombination(ArchitectureStyle architecture, DatabaseProvider database) =>
        !(architecture == ArchitectureStyle.Microservices && database == DatabaseProvider.SQLite);

    public static bool IsValidCombination(ArchitectureStyle architecture, bool includeAspire) =>
        !(architecture == ArchitectureStyle.Serverless && includeAspire);

    public static bool IsValidProjectName(string name) =>
        !string.IsNullOrWhiteSpace(name) &&
        char.IsLetter(name[0]) &&
        name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
}

internal sealed class OptionValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private OptionValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static OptionValidationResult Success() => new(true, []);
    public static OptionValidationResult Failure(IEnumerable<string> errors) => new(false, errors.ToList());
}

