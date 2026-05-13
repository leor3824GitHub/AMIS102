using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.CreateCapitalizationThreshold;

public sealed class CreateCapitalizationThresholdCommandValidator : AbstractValidator<CreateCapitalizationThresholdCommand>
{
    public CreateCapitalizationThresholdCommandValidator()
    {
        RuleFor(x => x.CircularName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.CapitalizationAmount)
            .GreaterThan(0)
            .WithMessage("Capitalization threshold must be greater than zero.");

        RuleFor(x => x.SemiExpendableLowValueThreshold)
            .GreaterThan(0)
            .WithMessage("Semi-expendable low-value threshold must be greater than zero.")
            .LessThan(x => x.CapitalizationAmount)
            .WithMessage("Semi-expendable low-value threshold must be less than the capitalization threshold.");

        RuleFor(x => x.EffectivityDate)
            .NotEmpty();
    }
}

