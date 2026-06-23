using System.Globalization;
using System.Text.RegularExpressions;

namespace AMIS.Maui.Services;

public sealed record StickerInfo(string? PropertyNo, string? Item, decimal? Value);

public static partial class PropertyNumberExtractor
{
    // SPLV-style: 2-5 letter prefix + 4-digit year + 2-digit segment + 4-digit serial (e.g. SPLV-2026-01-0001)
    // \b version — used against raw OCR text where word boundaries are reliable.
    [GeneratedRegex(@"\b[A-Z]{2,5}-\d{4}-\d{2}-\d{4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex SplvPropertyNoRegex();

    // NFA-style: year + 3-5 dash-separated alnum segments (e.g. 2023-NFA-00B-DP-D-056)
    [GeneratedRegex(@"\b\d{4}(?:-[A-Z0-9]{1,5}){3,5}\b", RegexOptions.IgnoreCase)]
    private static partial Regex NfaPropertyNoRegex();

    // Compact versions (no \b) — used after ALL whitespace is stripped.
    // Without surrounding spaces the \b boundary at the end fails (e.g. "0001E" — both word chars),
    // so these patterns rely on the specificity of the format alone.
    [GeneratedRegex(@"[A-Z]{2,5}-\d{4}-\d{2}-\d{4}", RegexOptions.IgnoreCase)]
    private static partial Regex SplvCompactRegex();

    [GeneratedRegex(@"\d{4}(?:-[A-Z0-9]{1,5}){3,5}", RegexOptions.IgnoreCase)]
    private static partial Regex NfaCompactRegex();

    // "Item: EPSON PRINTER L5290" — captures the rest of the line
    [GeneratedRegex(@"(?im)^\s*ITEM\s*[:\-]\s*(.+?)\s*$")]
    private static partial Regex ItemRegex();

    // "VALUE: Php 14,304.70" / "Php 14,304.70" / "₱ 14,304.70"
    [GeneratedRegex(@"(?i)(?:VALUE\s*[:\-]|PHP|₱)\s*[:\-]?\s*([\d]{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?|\d+\.\d{1,2})")]
    private static partial Regex ValueRegex();

    public static string? ExtractFirst(string? rawOcrText)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText)) return null;

        // Pass 1: raw text — \b word boundaries work correctly when whitespace is preserved.
        var match = NfaPropertyNoRegex().Match(rawOcrText);
        if (match.Success) return match.Value.ToUpperInvariant();
        match = SplvPropertyNoRegex().Match(rawOcrText);
        if (match.Success) return match.Value.ToUpperInvariant();

        // Pass 2: compact text — strip ALL whitespace (spaces, tabs, newlines) so OCR line-wraps
        // can't split the dash sequence, then use the no-\b regexes since word boundaries are gone.
        var compact = Regex.Replace(rawOcrText, @"\s+", "").ToUpperInvariant();
        match = NfaCompactRegex().Match(compact);
        if (match.Success) return match.Value;
        match = SplvCompactRegex().Match(compact);
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
