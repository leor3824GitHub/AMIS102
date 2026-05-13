using Spectre.Console;

namespace AMIS.CLI.UI;

internal static class ConsoleTheme
{
    // AMIS brand color
    public static Color Primary { get; } = new(62, 175, 124); // #3eaf7c
    public static Color Secondary { get; } = Color.White;
    public static Color Success { get; } = Color.Green;
    public static Color Warning { get; } = Color.Yellow;
    public static Color Error { get; } = Color.Red;
    public static Color Muted { get; } = Color.Grey;
    public static Color Dim { get; } = new(128, 128, 128);

    public static Style PrimaryStyle { get; } = new(Primary);
    public static Style SecondaryStyle { get; } = new(Secondary);
    public static Style SuccessStyle { get; } = new(Success);
    public static Style WarningStyle { get; } = new(Warning);
    public static Style ErrorStyle { get; } = new(Error);
    public static Style MutedStyle { get; } = new(Muted);
    public static Style DimStyle { get; } = new(Dim);

    public static void WriteBanner()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold {Primary.ToMarkup()}]AMIS[/] [dim]•[/] AMIS .NET Starter Kit");
        AnsiConsole.WriteLine();
    }

    public static void WriteSuccess(string message) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");

    public static void WriteError(string message) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");

    public static void WriteWarning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]![/] {message}");

    public static void WriteInfo(string message) =>
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {message}");

    public static void WriteStep(string message) =>
        AnsiConsole.MarkupLine($"  [dim]→[/] {message}");

    public static void WriteDone(string message) =>
        AnsiConsole.MarkupLine($"\n[green]Done![/] {message}");

    public static void WriteHeader(string message) =>
        AnsiConsole.MarkupLine($"\n[bold]{message}[/]");

    public static void WriteKeyValue(string key, string value, bool highlight = false) =>
        AnsiConsole.MarkupLine(highlight
            ? $"  [dim]{key}:[/] [{Primary.ToMarkup()}]{value}[/]"
            : $"  [dim]{key}:[/] {value}");
}

