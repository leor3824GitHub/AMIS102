using AMIS.Blazor.Services;
using MudBlazor;

namespace AMIS.Blazor.Components.Shared;

/// <summary>
/// Glue between <see cref="QuestPdfPageSetupDialog"/> and <see cref="IQuestPdfSetupPreferences"/>
/// so report pages don't each repeat the load-show-save plumbing.
/// </summary>
internal static class QuestPdfPageSetupDialogExtensions
{
    /// <summary>
    /// Loads the saved Page Setup for <paramref name="reportKey"/>, shows the QuestPDF Page
    /// Setup dialog seeded with it (falling back to the supplied defaults the first time),
    /// persists the chosen values on confirm, and returns the result. Returns <c>null</c> if
    /// the user cancels.
    /// </summary>
    public static async Task<QuestPdfPageSetupDialog.Result?> ShowSetupDialogAsync(
        this IQuestPdfSetupPreferences prefs,
        IDialogService dialogService,
        string reportKey,
        string defaultPaperSize = "a4",
        string defaultOrientation = "landscape",
        double defaultMarginMm = 15d)
    {
        var saved = await prefs.GetAsync(reportKey);
        var parameters = new DialogParameters
        {
            [nameof(QuestPdfPageSetupDialog.PaperSize)] = saved?.PaperSize ?? defaultPaperSize,
            [nameof(QuestPdfPageSetupDialog.Orientation)] = saved?.Orientation ?? defaultOrientation,
            [nameof(QuestPdfPageSetupDialog.MarginMm)] = saved?.MarginMm ?? defaultMarginMm,
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = false, CloseOnEscapeKey = true };

        var dialog = await dialogService.ShowAsync<QuestPdfPageSetupDialog>("Page Setup", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not QuestPdfPageSetupDialog.Result r)
            return null;

        await prefs.SaveAsync(reportKey, new QuestPdfSetup(r.PaperSize, r.Orientation, r.MarginMm));
        return r;
    }
}
