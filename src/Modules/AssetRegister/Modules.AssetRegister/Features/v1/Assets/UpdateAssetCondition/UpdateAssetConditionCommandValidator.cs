using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Assets;

namespace FSH.Modules.AssetRegister.Features.v1.Assets.UpdateAssetCondition;

public sealed class UpdateAssetConditionCommandValidator : AbstractValidator<UpdateAssetConditionCommand>
{
    public UpdateAssetConditionCommandValidator()
    {
        RuleFor(x => x.AssetRegistryId).NotEmpty().WithMessage("Asset registry ID is required.");
    }
}
