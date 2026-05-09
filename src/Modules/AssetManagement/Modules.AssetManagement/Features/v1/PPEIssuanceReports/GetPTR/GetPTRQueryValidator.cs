using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

public sealed class GetPTRQueryValidator : AbstractValidator<GetPTRQuery>
{
    public GetPTRQueryValidator()
    {
        RuleFor(q => q.PPEIRId)
            .NotEmpty();
    }
}