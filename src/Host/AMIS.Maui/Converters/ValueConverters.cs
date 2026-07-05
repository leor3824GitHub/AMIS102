using System.Globalization;

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
        value is int i ? i != 0 : false;

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
            return Resource(selected ? "White" : "Gray900");

        return selected ? Resource("Primary") : Colors.Transparent;
    }

    private static object Resource(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a document kind ("ICS" / "PAR") to a chip color drawn from app resources.
/// Pass ConverterParameter="bg" for the chip background, "text" for the label color.
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
            _ => wantBackground ? "PrimaryLight" : "Primary",
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true
            ? color
            : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
