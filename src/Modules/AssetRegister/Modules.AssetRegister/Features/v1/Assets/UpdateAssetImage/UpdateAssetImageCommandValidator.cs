using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetImage;

public sealed class UpdateAssetImageCommandValidator : AbstractValidator<UpdateAssetImageCommand>
{
    // Real image-size ceiling (decoded bytes). The server re-encodes and downscales anyway, but this
    // rejects absurd uploads before they hit the imaging pipeline. Client capture paths resize far below this.
    private const int MaxImageBytes = 8 * 1024 * 1024; // 8 MB

    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public UpdateAssetImageCommandValidator()
    {
        RuleFor(x => x.AssetRegistryId).NotEmpty().WithMessage("Asset registry ID is required.");

        // ImageUrl null/blank = clear the photo (always allowed). When present it must be an image data URL.
        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl!)
                .Must(BeImageDataUrl)
                .WithMessage("Image must be an image data URL (JPEG, PNG, WebP or GIF).")
                .Must(BeWithinSizeLimit)
                .WithMessage("Image is too large (max 8 MB).");
        });
    }

    private static bool BeImageDataUrl(string value)
    {
        if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;
        var contentType = ExtractContentType(value);
        return contentType is not null &&
               AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeWithinSizeLimit(string value)
    {
        var marker = value.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return false;
        // 4 base64 chars ≈ 3 bytes; estimate decoded size without allocating.
        var base64Length = value.Length - (marker + "base64,".Length);
        var approxBytes = base64Length / 4L * 3L;
        return approxBytes <= MaxImageBytes;
    }

    private static string? ExtractContentType(string value)
    {
        var marker = value.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return null;
        // "data:image/png;base64," → "image/png"
        var header = value[5..marker].TrimEnd(';');
        return string.IsNullOrWhiteSpace(header) ? null : header;
    }
}
