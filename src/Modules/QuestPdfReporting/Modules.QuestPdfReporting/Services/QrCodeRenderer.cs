using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace AMIS.Modules.QuestPdfReporting.Services;

/// <summary>
/// Encodes a string into a QR-code module matrix (via the core ZXing.Net writer — no
/// System.Drawing / SkiaSharp dependency) and draws it into a QuestPDF container as a crisp
/// vector grid of filled cells. Used to stamp a scannable Property No. on printed property stickers.
/// </summary>
public static class QrCodeRenderer
{
    /// <summary>
    /// Produces a row-major boolean grid (<c>rows[y][x]</c>) where <c>true</c> = dark module.
    /// Width/height of 0 tells ZXing to emit the minimal grid (one cell per module) including the
    /// quiet-zone margin.
    /// </summary>
    public static bool[][] Encode(string content, int marginModules = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.M,
            [EncodeHintType.MARGIN] = marginModules,
            [EncodeHintType.CHARACTER_SET] = "UTF-8",
        };

        var matrix = new QRCodeWriter().encode(content, BarcodeFormat.QR_CODE, 0, 0, hints);

        var rows = new bool[matrix.Height][];
        for (var y = 0; y < matrix.Height; y++)
        {
            var row = new bool[matrix.Width];
            for (var x = 0; x < matrix.Width; x++)
                row[x] = matrix[x, y];
            rows[y] = row;
        }

        return rows;
    }

    /// <summary>
    /// Draws <paramref name="content"/> as a QR code filling a <paramref name="sizePoints"/>-square
    /// box inside the supplied container. Rendered as a vector grid so it stays sharp at any print DPI.
    /// </summary>
    public static void DrawQr(this IContainer container, string content, float sizePoints)
    {
        ArgumentNullException.ThrowIfNull(container);

        var rows = Encode(content);
        var module = sizePoints / rows.Length;

        container.Width(sizePoints).Height(sizePoints).Column(col =>
        {
            foreach (var row in rows)
            {
                col.Item().Height(module).Row(cells =>
                {
                    foreach (var on in row)
                        cells.RelativeItem().Background(on ? Colors.Black : Colors.White);
                });
            }
        });
    }
}
