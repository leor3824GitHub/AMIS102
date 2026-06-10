using System.Globalization;

namespace AMIS.Playground.Blazor.Common;

/// <summary>
/// Single source of truth for currency display across the Blazor UI.
/// The culture — and therefore the currency symbol (₱ for en-PH) — is configured
/// once at startup from the "Localization:Culture" app setting (see Program.cs).
///
/// Use <see cref="ToCurrency(decimal)"/> / <see cref="Symbol"/> instead of hardcoding
/// the ₱ glyph in .razor files. Centralizing the symbol means changing the currency
/// is a one-line config change, and it removes the literal non-ASCII glyph that the
/// ANSI-save bug keeps corrupting to "?".
/// </summary>
public static class Money
{
    // Default keeps the app working even if the setting is missing.
    private static CultureInfo _culture = CultureInfo.GetCultureInfo("en-PH");

    /// <summary>Configure the currency culture once at startup from app config.</summary>
    public static void Configure(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;

        try
        {
            _culture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            // Unknown culture name in config — keep the en-PH default rather than throwing at startup.
        }
    }

    /// <summary>The configured currency symbol, e.g. "₱". Use in column headers and labels.</summary>
    public static string Symbol => _culture.NumberFormat.CurrencySymbol;

    /// <summary>Format an amount with symbol, e.g. "₱1,250.00".</summary>
    public static string ToCurrency(this decimal amount) => amount.ToString("C2", _culture);

    /// <summary>Format a nullable amount; renders <paramref name="fallback"/> when null.</summary>
    public static string ToCurrency(this decimal? amount, string fallback = "—") =>
        amount.HasValue ? amount.Value.ToString("C2", _culture) : fallback;

    /// <summary>Format the numeric part only — no symbol, e.g. "1,250.00" — for cells under a "(₱)" header.</summary>
    public static string ToAmount(this decimal amount) => amount.ToString("N2", _culture);

    /// <summary>Format a nullable amount with no symbol; renders <paramref name="fallback"/> when null.</summary>
    public static string ToAmount(this decimal? amount, string fallback = "—") =>
        amount.HasValue ? amount.Value.ToString("N2", _culture) : fallback;
}
