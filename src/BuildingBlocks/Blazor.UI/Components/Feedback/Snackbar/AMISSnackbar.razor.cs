using MudBlazor;

namespace AMIS.Framework.Blazor.UI.Components.Feedback.Snackbar;

/// <summary>
/// Convenience wrapper for snackbar calls with consistent styling.
/// </summary>
public sealed class AMISSnackbar
{
    private readonly ISnackbar _snackbar;

    public AMISSnackbar(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    public void Success(string message) => Add(message, Severity.Success);
    public void Info(string message) => Add(message, Severity.Info);
    public void Warning(string message) => Add(message, Severity.Warning);
    public void Error(string message) => Add(message, Severity.Error);

    private void Add(string message, Severity severity)
    {
        _snackbar.Add(message, severity);
    }
}

