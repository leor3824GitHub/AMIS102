using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetImage;

public sealed class UpdateAssetImageCommandValidator : AbstractValidator<UpdateAssetImageCommand>
{
    // Matches the ImageUrl column cap (10 MB) — enough for a base64-encoded photo (~7.6 MB image).
    private const int MaxImageUrlLength = 10_000_000;

    public UpdateAssetImageCommandValidator()
    {
        RuleFor(x => x.AssetRegistryId).NotEmpty().WithMessage("Asset registry ID is required.");
        RuleFor(x => x.ImageUrl!)
            .MaximumLength(MaxImageUrlLength).WithMessage("Image is too large.")
            .When(x => x.ImageUrl is not null);
    }
}
