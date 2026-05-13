using System.Globalization;

namespace AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

/// <summary>
/// COA Circular 2020-006 property number: YYYY-AA-BB-NNNN-CC.
/// YYYY = acquisition year, AA = sub-major account, BB = GL account,
/// NNNN = serial, CC = location code.
/// </summary>
public sealed record PropertyNumber
{
    public string Value { get; }
    public int Year { get; }
    public string SubMajor { get; }
    public string GlAccount { get; }
    public string Serial { get; }
    public string Location { get; }

    private PropertyNumber(string value, int year, string subMajor, string glAccount, string serial, string location)
    {
        Value = value;
        Year = year;
        SubMajor = subMajor;
        GlAccount = glAccount;
        Serial = serial;
        Location = location;
    }

    public static PropertyNumber Create(int year, string subMajor, string glAccount, int serial, string location)
    {
        if (year < 1900 || year > 2999)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be a 4-digit value.");
        if (subMajor is null || subMajor.Length != 2)
            throw new ArgumentException("SubMajor must be exactly 2 characters.", nameof(subMajor));
        if (glAccount is null || glAccount.Length != 2)
            throw new ArgumentException("GlAccount must be exactly 2 characters.", nameof(glAccount));
        if (location is null || location.Length != 2)
            throw new ArgumentException("Location must be exactly 2 characters.", nameof(location));
        if (serial < 0 || serial > 9999)
            throw new ArgumentOutOfRangeException(nameof(serial), "Serial must be a 4-digit value.");

        var serialStr = serial.ToString("D4", CultureInfo.InvariantCulture);
        var value = $"{year:D4}-{subMajor}-{glAccount}-{serialStr}-{location}";
        return new PropertyNumber(value, year, subMajor, glAccount, serialStr, location);
    }

    public static PropertyNumber Parse(string s)
    {
        if (!TryParse(s, out var pn))
            throw new FormatException($"'{s}' is not a valid COA 2020-006 property number.");
        return pn!;
    }

    public static bool TryParse(string? s, out PropertyNumber? propertyNumber)
    {
        propertyNumber = null;
        if (string.IsNullOrWhiteSpace(s)) return false;

        var parts = s.Split('-');
        if (parts.Length != 5) return false;
        if (parts[0].Length != 4 || parts[1].Length != 2 || parts[2].Length != 2
            || parts[3].Length != 4 || parts[4].Length != 2) return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)) return false;
        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var serial)) return false;

        propertyNumber = new PropertyNumber(s, year, parts[1], parts[2], parts[3], parts[4]);
        return true;
    }

    public override string ToString() => Value;
}

