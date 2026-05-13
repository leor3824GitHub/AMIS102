using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.GenerateRegSPIPdf;

public sealed class GenerateRegSPIPdfCommandValidator : AbstractValidator<GenerateRegSPIPdfCommand>
{
    public GenerateRegSPIPdfCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty();

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 5000);
    }
}
