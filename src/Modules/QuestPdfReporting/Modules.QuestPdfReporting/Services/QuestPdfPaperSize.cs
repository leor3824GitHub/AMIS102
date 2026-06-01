using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Services;

/// <summary>
/// Canonical paper sizes (in mm) for AMIS government forms rendered via QuestPDF,
/// plus a single <see cref="Apply"/> entry point that sets the page size + orientation
/// on a QuestPDF <see cref="PageDescriptor"/>. Mirrors <c>FastReportPaperSize</c> in
/// Modules.FastReporting — values are duplicated by design (QuestPDF takes mm directly
/// via <see cref="Unit.Millimetre"/>, FastReport uses float mm on each ReportPage).
/// </summary>
public static class QuestPdfPaperSize
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
    /// Applies the resolved paper size and orientation to a QuestPDF page, and
    /// (optionally) a uniform page margin in millimetres. Call inside the
    /// <c>container.Page(page =&gt; ...)</c> lambda in place of
    /// <c>page.Size(PageSizes.A4.Landscape())</c> + <c>page.Margin(...)</c>.
    /// Safe to call once per page. When <paramref name="marginMm"/> is null the
    /// page margin is left untouched (caller may set it separately).
    /// </summary>
    public static void Apply(PageDescriptor page, string? paperSize, string? orientation, float? marginMm = null)
    {
        ArgumentNullException.ThrowIfNull(page);

        var (longSide, shortSide) = Resolve(paperSize);

        if (IsLandscape(orientation))
            page.Size(longSide, shortSide, Unit.Millimetre);
        else
            page.Size(shortSide, longSide, Unit.Millimetre);

        if (marginMm is > 0f)
            page.Margin(marginMm.Value, Unit.Millimetre);
    }
}
