using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Depreciation.RunDepreciation;

public sealed class RunDepreciationCommandValidator : AbstractValidator<RunDepreciationCommand>
{
    public RunDepreciationCommandValidator()
    {
        When(x => x.AsOfPeriod.HasValue, () =>
        {
            RuleFor(x => x.AsOfPeriod!.Value)
                .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
                .WithMessage("AsOfPeriod must be on or after 2000-01-01.");
        });
    }
}
