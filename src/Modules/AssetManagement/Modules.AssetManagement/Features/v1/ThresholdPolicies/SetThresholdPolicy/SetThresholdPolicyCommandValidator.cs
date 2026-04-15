using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.SetThresholdPolicy;

public sealed class SetThresholdPolicyCommandValidator : AbstractValidator<SetThresholdPolicyCommand>
{
    public SetThresholdPolicyCommandValidator()
    {
        RuleFor(x => x.LowValueThreshold)
            .GreaterThan(0)
            .WithMessage("Low-value threshold must be greater than zero.");

        RuleFor(x => x.CapitalizationThreshold)
            .GreaterThan(0)
            .WithMessage("Capitalization threshold must be greater than zero.");

        RuleFor(x => x)
            .Must(x => x.LowValueThreshold < x.CapitalizationThreshold)
            .WithName("LowValueThreshold")
            .WithMessage("Low-value threshold must be less than the capitalization threshold.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters.");
    }
}
