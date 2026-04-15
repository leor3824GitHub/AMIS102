using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

public sealed class CreateRRPCommandValidator : AbstractValidator<CreateRRPCommand>
{
    public CreateRRPCommandValidator()
    {
        RuleFor(x => x.RRPNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.ReturnCategory).IsInEnum();
        RuleFor(x => x.ReturnedByEmployeeId).NotEmpty();
        RuleFor(x => x.ApprovedByEmployeeId).NotEmpty();
        RuleFor(x => x.SignedByEmployeeId).NotEmpty();
        RuleFor(x => x.PropertyInspectorCertified)
            .Equal(true)
            .WithMessage("Property Inspector Certification is required before processing an RRP.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.PPEItemId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.SourceDocumentRef).MaximumLength(64);
        });
    }
}
