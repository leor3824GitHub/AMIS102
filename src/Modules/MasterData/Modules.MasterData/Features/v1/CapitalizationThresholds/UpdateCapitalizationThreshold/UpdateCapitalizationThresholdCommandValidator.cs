using FluentValidation;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.UpdateCapitalizationThreshold;

public sealed class UpdateCapitalizationThresholdCommandValidator : AbstractValidator<UpdateCapitalizationThresholdCommand>
{
    public UpdateCapitalizationThresholdCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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
