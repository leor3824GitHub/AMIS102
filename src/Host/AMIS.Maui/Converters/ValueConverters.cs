using System.Globalization;
using AMIS.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AMIS.Maui.Converters;

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IsNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IsNotZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i != 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

/// <summary>
/// Returns true when the bound string equals the ConverterParameter (case-insensitive,
/// trimmed). Used to show/hide controls based on a status value, e.g. the Accept button
/// only when Status == "PendingAcceptance".
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals((value as string)?.Trim(), (parameter as string)?.Trim(), StringComparison.OrdinalIgnoreCase);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Drives a segmented-control chip's background/text color from the currently selected segment,
/// so the selected state is reliable. A selected-state <c>DataTrigger</c> can't dependably override
/// a base <c>BackgroundColor</c>/<c>TextColor</c> in MAUI — the classic symptom is a blank chip
/// (white text on a light pill). Binding the color through this converter avoids triggers entirely.
///
/// <para>ConverterParameter form: <c>"{segmentValue}|{role}"</c> where role is <c>bg</c> or <c>text</c>.
/// The bound value is the currently selected segment. Selected → Primary background / White text;
/// unselected → transparent background (shows the track) / Gray900 text (legible on both themes).</para>
/// </summary>
public sealed class SegmentColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string)?.Split('|');
        var segment = parts is { Length: > 0 } ? parts[0] : null;
        var role = parts is { Length: > 1 } ? parts[1] : "bg";
        var selected = string.Equals((value as string)?.Trim(), segment?.Trim(), StringComparison.OrdinalIgnoreCase);

        if (role.Equals("text", StringComparison.OrdinalIgnoreCase))
            return Resource(selected ? "White" : "Gray500");

        return selected ? Resource("Primary") : Colors.Transparent;
    }

    private static object Resource(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Resolves one of two app-resource colors from a bool, so a two-state toggle's colors are
/// binding-driven rather than <c>DataTrigger</c>-driven (a trigger can't reliably override a base
/// color in MAUI, which can leave a chip rendering blank). ConverterParameter form:
/// <c>"{trueKey}|{falseKey}"</c> naming resource colors; the literal token <c>Transparent</c> (either
/// side) maps to a transparent color.
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string)?.Split('|');
        var trueKey = parts is { Length: > 0 } ? parts[0] : null;
        var falseKey = parts is { Length: > 1 } ? parts[1] : null;
        var key = value is true ? trueKey : falseKey;

        if (string.IsNullOrEmpty(key) || key.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            return Colors.Transparent;

        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Turns an asset photo string into an <see cref="ImageSource"/> for an <c>Image.Source</c> binding.
/// The API returns either a base64 data URL (<c>data:image/jpeg;base64,…</c>) or an absolute http URL;
/// MAUI's <c>Image</c> can load the latter but not the former, so data URLs are decoded to a stream
/// source here. Returns <c>null</c> for null/blank/unparseable input so the row falls back to its
/// placeholder tile instead of throwing.
/// </summary>
public sealed class ImageUrlToSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;

        if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var marker = s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
                return null;

            try
            {
                var bytes = System.Convert.FromBase64String(s[(marker + "base64,".Length)..]);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return Uri.TryCreate(s, UriKind.Absolute, out var uri) ? ImageSource.FromUri(uri) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Lazily loads a list row's asset photo through the authenticated API, so the multi-MB base64
/// blob no longer rides inside the list payload. Bound to the whole row via <see cref="IAssetImageRow"/>
/// (ICS/PAR detail lines and the physical-count checklist alike): returns null (→ placeholder tile)
/// when the asset has no photo, otherwise an <see cref="ImageSource"/> that streams the bytes from
/// <c>GET /assets/{id}/image</c> via the DI <see cref="IApiClient"/> (bearer token attached by the
/// authenticated handler — a plain <c>Uri</c> source could not carry it). A failed fetch degrades to
/// the placeholder rather than crashing the list.
/// </summary>
public sealed class AssetImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IAssetImageRow item || !item.HasImage)
            return null;

        var api = IPlatformApplication.Current?.Services?.GetService<IApiClient>();
        if (api is null)
            return null;

        var id = item.AssetRegistryId;
        return ImageSource.FromStream(async ct =>
        {
            try
            {
                // List rows only need the small thumbnail, not the full image.
                return await api.GetAssetImageStreamAsync(id, "thumb", ct) ?? Stream.Null;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
            {
                // Thumbnail is non-critical — fall back to the placeholder tile instead of crashing.
                return Stream.Null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a message's "is mine" flag to a horizontal alignment so sent bubbles hug the right and
/// received bubbles hug the left. true → End, false → Start.
/// </summary>
public sealed class BoolToLayoutOptionsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? LayoutOptions.End : LayoutOptions.Start;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Theme-aware bubble colors for the chat conversation, keyed off the message's "is mine" flag.
/// A DataTrigger can't override an AppThemeBinding base in MAUI, so colors are resolved here instead.
/// ConverterParameter <c>bg</c> → bubble background (mine=Primary, theirs=card surface);
/// <c>text</c> → body text color (mine=white, theirs=primary text). Reads the current theme so both
/// modes look right (rebuilt on reload; live theme-toggle mid-scroll is not a target case).
/// </summary>
public sealed class ChatBubbleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var mine = value is true;
        var wantText = string.Equals(parameter as string, "text", StringComparison.OrdinalIgnoreCase);
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;

        var key = (wantText, mine, dark) switch
        {
            (true, true, _) => "White",
            (true, false, true) => "White",
            (true, false, false) => "PrimaryText",
            (false, true, true) => "PrimaryDark",
            (false, true, false) => "Primary",
            (false, false, true) => "Gray950",
            (false, false, false) => "White",
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Extracts up to two uppercase initials from a name for avatar circles. "Supply Office" → "SO",
/// "Juan dela Cruz" → "JC", single word → its first letter. Blank input → "?".
/// </summary>
public sealed class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s))
            return "?";

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0][..1].ToUpperInvariant();

        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a physical-count entry Result to a solid badge color (white text on top).
/// Found→Success, NotFound→Danger, FoundAtStation→Primary, Queued→Info, else→Gray400.
/// </summary>
public sealed class EntryResultColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value as string)?.Trim() switch
        {
            "Found" => "Success",
            "NotFound" => "Danger",
            "FoundAtStation" => "Primary",
            "Queued" => "Info",
            _ => "Gray400",
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a physical-count entry Result to a short display label:
/// NotFound→"Missing", FoundAtStation→"@Station", null/empty→"Pending", else the value as-is.
/// </summary>
public sealed class EntryResultLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim() switch
        {
            "NotFound" => "Missing",
            "FoundAtStation" => "@Station",
            null or "" => "Pending",
            var s => s,
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a physical-count session status to a solid badge color (white text sits on top).
/// Ongoing→Warning, Reconciled→Primary, Closed/Reconciled(final)→Success, anything else→Gray400.
/// Replaces per-badge DataTriggers so the color is a single binding.
/// </summary>
public sealed class CountStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value as string)?.Trim() switch
        {
            "Ongoing" => "Warning",
            "Reconciled" => "Primary",
            "Closed" => "Success",
            _ => "Gray400",
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Deterministically maps a name/string to one of a fixed set of avatar-circle background colors,
/// so a channel or person always gets the same color without storing one. Hashes the trimmed,
/// lower-cased string and indexes into the semantic palette. Returns a mid-tone color intended to
/// carry white initials on top.
/// </summary>
public sealed class NameToColorConverter : IValueConverter
{
    private static readonly string[] Palette =
        ["Primary", "Teal", "Info", "Success", "Tertiary", "Magenta"];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s))
            return Resource("Primary");

        // Stable, framework-independent hash (string.GetHashCode is randomized per run).
        var hash = 0;
        foreach (var c in s)
            hash = unchecked((hash * 31) + c);

        var key = Palette[Math.Abs(hash) % Palette.Length];
        return Resource(key);
    }

    private static object Resource(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a document kind or asset type to a chip color drawn from app resources.
/// Document kinds: "ICS" (primary) / "PAR" (teal). Asset types: "PPE" (info/blue) / "SE" (teal) —
/// used to tint the checklist thumbnail so PPE and SE items read apart at a glance.
/// Pass ConverterParameter="bg" for the chip/tile background, "text" for the glyph/label color.
/// </summary>
public sealed class KindToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = (value as string)?.Trim().ToUpperInvariant();
        var wantBackground = string.Equals(parameter as string, "bg", StringComparison.OrdinalIgnoreCase);

        var key = kind switch
        {
            "PAR" => wantBackground ? "TealLight" : "Teal",
            "PPE" => wantBackground ? "InfoLight" : "Info",
            "SE" => wantBackground ? "TealLight" : "Teal",
            _ => wantBackground ? "PrimaryLight" : "Primary",
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true
            ? color
            : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
