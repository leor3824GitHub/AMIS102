namespace AMIS.Modules.QuestPdfReporting.Services;

/// <summary>
/// Bundled agency logo, loaded once from any image embedded under the module's ReportAssets\ folder
/// (ReportAssets\**\*.png|jpg are auto-embedded — see Modules.QuestPdfReporting.csproj). Prefers an asset whose
/// name mentions "logo". <see cref="Bytes"/> is null when no asset exists, so headers degrade to text only.
/// </summary>
internal static class ReportLogo
{
    private static readonly Lazy<byte[]?> Cached = new(Load);

    public static byte[]? Bytes => Cached.Value;

    private static byte[]? Load()
    {
        var asm = typeof(ReportLogo).Assembly;
        var images = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".ReportAssets.", StringComparison.OrdinalIgnoreCase)
                && (n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var name = images.FirstOrDefault(n => n.Contains("logo", StringComparison.OrdinalIgnoreCase))
                   ?? images.FirstOrDefault();
        if (name is null)
            return null;

        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
