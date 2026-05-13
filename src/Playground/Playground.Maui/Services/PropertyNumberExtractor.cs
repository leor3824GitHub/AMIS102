using System.Globalization;
using System.Text.RegularExpressions;

namespace Playground.Maui.Services;

public sealed record StickerInfo(string? PropertyNo, string? Item, decimal? Value);

public static partial class PropertyNumberExtractor
{
    // SPLV-style: 2-5 letter prefix + 4-digit year + 2-digit segment + 4-digit serial (e.g. SPLV-2026-01-0001)
    [GeneratedRegex(@"\b[A-Z]{2,5}-\d{4}-\d{2}-\d{4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex SplvPropertyNoRegex();

    // NFA-style: year + 3-5 dash-separated alnum segments (e.g. 2023-NFA-00B-DP-D-056)
    [GeneratedRegex(@"\b\d{4}(?:-[A-Z0-9]{1,5}){3,5}\b", RegexOptions.IgnoreCase)]
    private static partial Regex NfaPropertyNoRegex();

    // "Item: EPSON PRINTER L5290" — captures the rest of the line
    [GeneratedRegex(@"(?im)^\s*ITEM\s*[:\-]\s*(.+?)\s*$")]
    private static partial Regex ItemRegex();

    // "VALUE: Php 14,304.70" / "Php 14,304.70" / "₱ 14,304.70"
    [GeneratedRegex(@"(?i)(?:VALUE\s*[:\-]|PHP|₱)\s*[:\-]?\s*([\d]{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+\.\d{1,2})")]
    private static partial Regex ValueRegex();

    public static string? ExtractFirst(string? rawOcrText)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText)) return null;

        // Strip whitespace so OCR line wraps don't break the dash sequence.
        var compact = rawOcrText.Replace(" ", "").Replace("\t", "").ToUpperInvariant();

        var match = NfaPropertyNoRegex().Match(compact);
        if (match.Success) return match.Value;

        match = SplvPropertyNoRegex().Match(compact);
        return match.Success ? match.Value : null;
    }

    public static StickerInfo Extract(string? rawOcrText)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText))
            return new StickerInfo(null, null, null);

        var propertyNo = ExtractFirst(rawOcrText);

        string? item = null;
        var itemMatch = ItemRegex().Match(rawOcrText);
        if (itemMatch.Success)
        {
            var raw = itemMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(raw)) item = raw;
        }

        decimal? value = null;
        var valueMatch = ValueRegex().Match(rawOcrText);
        if (valueMatch.Success)
        {
            var numeric = valueMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                value = parsed;
        }

        return new StickerInfo(propertyNo, item, value);
    }
}
