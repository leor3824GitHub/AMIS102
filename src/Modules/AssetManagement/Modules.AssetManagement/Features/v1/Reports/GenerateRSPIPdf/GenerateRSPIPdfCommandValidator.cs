using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.GenerateRSPIPdf;

public sealed class GenerateRSPIPdfCommandValidator : AbstractValidator<GenerateRSPIPdfCommand>
{
    public GenerateRSPIPdfCommandValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 5000);

        RuleFor(x => x)
            .Must(x => x.DateFrom is null || x.DateTo is null || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom cannot be later than DateTo.");
    }
}

