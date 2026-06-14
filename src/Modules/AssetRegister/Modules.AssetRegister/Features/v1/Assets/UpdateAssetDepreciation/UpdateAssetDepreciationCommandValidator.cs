using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetDepreciation;

public sealed class UpdateAssetDepreciationCommandValidator : AbstractValidator<UpdateAssetDepreciationCommand>
{
    public UpdateAssetDepreciationCommandValidator()
    {
        RuleFor(x => x.AssetRegistryId).NotEmpty();
        RuleFor(x => x.ResidualValue).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EstimatedUsefulLifeYears).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
