using FastReport;

namespace AMIS.Modules.FastReporting.Services;

/// <summary>
/// Canonical paper sizes (in mm) for AMIS government forms rendered via FastReport,
/// plus a single <see cref="Apply"/> entry point that mutates a loaded <see cref="Report"/>
/// in place. Mirrors <c>GovernmentPaperSizes</c> in Modules.RdlcReporting — values are
/// duplicated by design (FastReport uses float mm, RDLC uses string dimensions).
/// </summary>
public static class FastReportPaperSize
{
    public const string A4 = "a4";
    public const string Legal = "legal";
    public const string LongBond = "longbond";
    public const string Letter = "letter";

    public const string Landscape = "landscape";
    public const string Portrait = "portrait";

    // Long side × short side, in millimetres.
    private static readonly (float LongMm, float ShortMm) A4Mm       = (297f,   210f);
    private static readonly (float LongMm, float ShortMm) LegalMm    = (355.6f, 215.9f); // 14 × 8.5 in
    private static readonly (float LongMm, float ShortMm) LongBondMm = (330.2f, 215.9f); // 13 × 8.5 in
    private static readonly (float LongMm, float ShortMm) LetterMm   = (279.4f, 215.9f); // 11 × 8.5 in

    public static (float LongMm, float ShortMm) Resolve(string? paperSize) =>
        (paperSize ?? string.Empty).ToLowerInvariant() switch
        {
            Legal    => LegalMm,
            LongBond => LongBondMm,
            Letter   => LetterMm,
            _        => A4Mm,
        };

    public static bool IsLandscape(string? orientation) =>
        !string.Equals(orientation, Portrait, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Applies the resolved paper size and orientation to every <see cref="ReportPage"/>
    /// in the supplied report. Safe to call multiple times.
    /// </summary>
    public static void Apply(Report report, string? paperSize, string? orientation)
    {
        ArgumentNullException.ThrowIfNull(report);

        var (longSide, shortSide) = Resolve(paperSize);
        var landscape = IsLandscape(orientation);

        foreach (var page in report.Pages.OfType<ReportPage>())
        {
            // Order matters: setting Landscape swaps Width/Height when its value flips.
            // Set the flag first, then assign dimensions in the resulting orientation.
            page.Landscape = landscape;
            if (landscape)
            {
                page.PaperWidth = longSide;
                page.PaperHeight = shortSide;
            }
            else
            {
                page.PaperWidth = shortSide;
                page.PaperHeight = longSide;
            }
        }
    }
}
