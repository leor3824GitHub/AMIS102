namespace AMIS.Modules.Expendable.Data.Services;

/// <summary>
/// Helpers for the base64 image data URLs the product create/update flow accepts from the client.
/// A stored <c>Product.ImageUrl</c> is a short storage key; only a <c>data:…;base64,</c> value is a new
/// upload to persist.
/// </summary>
public static class ProductImageDataUrl
{
    /// <summary>True when the value is a base64 image data URL (a new upload), not a stored key.</summary>
    public static bool IsDataUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>Decodes a base64 image data URL to raw bytes, or null if the value isn't a data URL.</summary>
    public static byte[]? Decode(string? value)
    {
        if (!IsDataUrl(value)) return null;
        var marker = value!.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return null;
        try { return Convert.FromBase64String(value[(marker + "base64,".Length)..]); }
        catch (FormatException) { return null; }
    }
}
