using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf.IO;

namespace AMIS.Modules.BudgetDisbursement.Services;

/// <summary>
/// Stamps each page of a PDF with the downloader's identity and access timestamp so that
/// leaked copies can be traced back to the person who downloaded them.
/// The original stored file is never modified — the watermark is applied on-the-fly at serve time.
/// </summary>
internal static class PdfWatermarkService
{
    static PdfWatermarkService()
    {
        // PdfSharp 6.x requires an explicit font resolver in server/ASP.NET contexts.
        // This runs once (CLR static constructor guarantee) before any font is used.
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
    }

    internal static byte[] Stamp(byte[] pdfBytes, string downloaderName, DateTimeOffset accessedAt)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

        var largeFont = new XFont("Arial", 28, XFontStyleEx.Bold);
        var smallFont = new XFont("Arial", 9, XFontStyleEx.Regular);
        var brush = new XSolidBrush(XColor.FromArgb(45, 0, 0, 0));
        var footerBrush = new XSolidBrush(XColor.FromArgb(90, 80, 80, 80));

        var line1 = downloaderName.ToUpperInvariant();
        var line2 = $"Downloaded: {accessedAt:yyyy-MM-dd HH:mm} UTC  |  FOR REFERENCE ONLY";

        foreach (var page in document.Pages)
        {
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

            var cx = page.Width.Point / 2;
            var cy = page.Height.Point / 2;

            // Diagonal name stamp across the center
            var state = gfx.Save();
            gfx.TranslateTransform(cx, cy);
            gfx.RotateTransform(-40);
            gfx.DrawString(line1, largeFont, brush,
                new XRect(-200, -22, 400, 30), XStringFormats.Center);
            gfx.Restore(state);

            // Small footer line at the bottom of the page
            gfx.DrawString(line2, smallFont, footerBrush,
                new XRect(10, page.Height.Point - 18, page.Width.Point - 20, 14),
                XStringFormats.CenterLeft);
        }

        using var outputStream = new MemoryStream();
        document.Save(outputStream);
        return outputStream.ToArray();
    }

    // Reads font files directly from the Windows Fonts directory.
    // Covers the four Arial variants used by the watermark (regular, bold, italic, bold-italic).
    private sealed class WindowsFontResolver : IFontResolver
    {
        private static readonly string FontsDir =
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        public string DefaultFontName => "Arial";

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
        {
            var suffix = (bold, italic) switch
            {
                (true, true)  => "bi",
                (true, false) => "bd",
                (false, true) => "i",
                _             => ""
            };
            return new FontResolverInfo($"{familyName.ToLowerInvariant()}{suffix}");
        }

        public byte[]? GetFont(string faceName)
        {
            var fileName = faceName switch
            {
                "arialbd" => "arialbd.ttf",
                "ariali"  => "ariali.ttf",
                "arialbi" => "arialbi.ttf",
                _         => $"{faceName}.ttf"
            };

            var path = Path.Combine(FontsDir, fileName);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
