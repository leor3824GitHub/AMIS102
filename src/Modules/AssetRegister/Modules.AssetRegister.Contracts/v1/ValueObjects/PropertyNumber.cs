namespace AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

/// <summary>
/// NFA property number — operator-assigned identifier shown on property stickers.
/// Format follows NFA's local policy (composed via the PropertyNoField generator);
/// this value object enforces only string-level invariants and lets the policy
/// define structure.
/// </summary>
public sealed record PropertyNumber
{
    public const int MaxLength = 32;

    public string Value { get; }

    private PropertyNumber(string value) => Value = value;

    public static PropertyNumber Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("PropertyNumber cannot be empty.", nameof(raw));

        var v = raw.Trim().ToUpperInvariant();
        if (v.Length > MaxLength)
            throw new ArgumentException($"PropertyNumber max length is {MaxLength} (got {v.Length}).", nameof(raw));

        return new PropertyNumber(v);
    }

    public static PropertyNumber Parse(string raw) => Create(raw);

    public static bool TryParse(string? raw, out PropertyNumber? propertyNumber)
    {
        propertyNumber = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        try { propertyNumber = Create(raw); return true; }
        catch (ArgumentException) { return false; }
    }

    public override string ToString() => Value;
}
