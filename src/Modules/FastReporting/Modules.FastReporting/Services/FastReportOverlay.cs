using FastReport;

namespace AMIS.Modules.FastReporting.Services;

/// <summary>
/// "Data-only" overlay rendering for pre-printed accountable forms (e.g. PPERR / PPEIR).
/// Hides every static element — labels, boxes, grid lines, column headers, the title, the
/// org letterhead, and page numbers — so only the variable field values print. The result
/// is a PDF that can be fed onto a pre-printed government form: the values land where the
/// template already positions them, and the borders are dropped so the printed grid is not
/// doubled on top of the pre-printed grid.
/// </summary>
public static class FastReportOverlay
{
    /// <summary>
    /// Mutates a loaded report so only data-bound values print. Call from a handler's
    /// <c>configureReport</c> hook, after paper-size setup.
    /// </summary>
    /// <param name="report">The loaded report.</param>
    /// <param name="dataSourceNames">
    /// Registered data source names whose <c>[Name.Field]</c> values should print
    /// (e.g. "RrDS", "LineItemsDS"). A TextObject prints only if it references one of these.
    /// </param>
    /// <param name="suppressFields">
    /// Field tokens kept out of the overlay even though they are data-bound — typically the
    /// letterhead ("OrgName", "OrgAddress"), which is already pre-printed on the form.
    /// </param>
    /// <param name="offsetXmm">Horizontal calibration nudge in millimetres (+ shifts right).</param>
    /// <param name="offsetYmm">Vertical calibration nudge in millimetres (+ shifts down).</param>
    public static void ApplyDataOnly(
        Report report,
        IReadOnlyCollection<string> dataSourceNames,
        IReadOnlyCollection<string>? suppressFields = null,
        float offsetXmm = 0f,
        float offsetYmm = 0f)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(dataSourceNames);

        suppressFields ??= [];

        foreach (var obj in report.AllObjects)
        {
            switch (obj)
            {
                // Bands are layout containers — keep them so their geometry (and the data
                // values they host) is preserved. Their static children are hidden below.
                case BandBase:
                    continue;

                // A data-bound value: print it, but drop the box so the pre-printed grid is
                // not doubled. Non-data TextObjects fall through to the hide branch.
                case TextObject text when IsDataValue(text.Text, dataSourceNames, suppressFields):
                    text.Border.Lines = BorderLines.None;
                    break;

                // Everything else on a band (labels, empty bordered cells, lines, the title,
                // letterhead, page numbers) is part of the pre-printed form — hide it.
                case ReportComponentBase component:
                    component.Visible = false;
                    break;
            }
        }

        ApplyOffset(report, offsetXmm, offsetYmm);
    }

    private static bool IsDataValue(
        string? text,
        IReadOnlyCollection<string> dataSourceNames,
        IReadOnlyCollection<string> suppressFields)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        if (suppressFields.Any(f => text.Contains(f, StringComparison.Ordinal)))
            return false;
        return dataSourceNames.Any(ds => text.Contains($"[{ds}.", StringComparison.Ordinal));
    }

    // Shift the whole page by nudging its margins (mm). Margins move all content uniformly
    // without disturbing the calibrated per-object coordinates inside the bands.
    private static void ApplyOffset(Report report, float offsetXmm, float offsetYmm)
    {
        if (offsetXmm == 0f && offsetYmm == 0f)
            return;

        foreach (var page in report.Pages.OfType<ReportPage>())
        {
            page.LeftMargin += offsetXmm;
            page.TopMargin += offsetYmm;
        }
    }
}
