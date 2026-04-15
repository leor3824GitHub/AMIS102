using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;

public sealed class CreatePPEIRCommandValidator : AbstractValidator<CreatePPEIRCommand>
{
    public CreatePPEIRCommandValidator()
    {
        RuleFor(x => x.PPEIRNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.IssuedToEmployeeId).NotEmpty();
        RuleFor(x => x.IssuedToOfficeAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IssuanceType).IsInEnum();
        RuleFor(x => x.IssuedByEmployeeId).NotEmpty();
        RuleFor(x => x.ReceivedByEmployeeId).NotEmpty();
        RuleFor(x => x.ApprovedByEmployeeId).NotEmpty();
        RuleFor(x => x.DriverName).MaximumLength(200);
        RuleFor(x => x.BillOfLadingNo).MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one PPE item is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
            item.RuleFor(x => x.PPEItemId).NotEmpty());
    }
}
