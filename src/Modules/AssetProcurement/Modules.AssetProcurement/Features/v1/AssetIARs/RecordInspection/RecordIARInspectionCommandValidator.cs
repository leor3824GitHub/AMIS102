using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FluentValidation;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.RecordInspection;

public sealed class RecordIARInspectionCommandValidator : AbstractValidator<RecordIARInspectionCommand>
{
    public RecordIARInspectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Decisions).NotEmpty().WithMessage("At least one inspection decision is required.");
        RuleForEach(x => x.Decisions).ChildRules(d =>
        {
            d.RuleFor(x => x.ItemNo).GreaterThan(0);
            d.RuleFor(x => x.Result).IsInEnum()
                .NotEqual(LineInspectionResult.Pending)
                .WithMessage("Each line must be either Passed or Rejected.");
            d.RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500)
                .When(x => x.Result == LineInspectionResult.Rejected)
                .WithMessage("Remarks are required when rejecting a line.");
        });
    }
}
