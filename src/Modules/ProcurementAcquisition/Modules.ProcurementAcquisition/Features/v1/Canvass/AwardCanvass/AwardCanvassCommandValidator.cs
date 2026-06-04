using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;

public sealed class AwardCanvassCommandValidator : AbstractValidator<AwardCanvassCommand>
{
    public AwardCanvassCommandValidator()
    {
        RuleFor(x => x.CanvassRequestId).NotEmpty();

        RuleFor(x => x.LineAwards)
            .NotEmpty().WithMessage("At least one line award is required.");

        RuleForEach(x => x.LineAwards).ChildRules(la =>
        {
            la.RuleFor(a => a.PrItemNo).GreaterThan(0);
            la.RuleFor(a => a.QuotationId).NotEmpty();
        });

        // A PR line may be awarded only once within a single award request.
        RuleFor(x => x.LineAwards)
            .Must(awards => awards.Select(a => a.PrItemNo).Distinct().Count() == awards.Count)
            .WithMessage("Each line may be awarded to only one supplier.")
            .When(x => x.LineAwards is { Count: > 0 });
    }
}

