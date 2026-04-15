using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.UpdatePPEIRDepreciation;

public sealed class UpdatePPEIRDepreciationCommandValidator : AbstractValidator<UpdatePPEIRDepreciationCommand>
{
    public UpdatePPEIRDepreciationCommandValidator()
    {
        RuleFor(x => x.PPEIRId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item must be provided.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ItemId).NotEmpty();
            item.RuleFor(x => x.AccumulatedDepreciation)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Accumulated depreciation must be zero or greater.");
            item.RuleFor(x => x.BookValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Book value must be zero or greater.");
        });
    }
}
