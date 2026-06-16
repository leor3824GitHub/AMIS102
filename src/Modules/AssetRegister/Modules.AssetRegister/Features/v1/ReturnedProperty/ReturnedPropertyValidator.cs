using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;

namespace AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;

public sealed class CreateReturnedPropertyReceiptCommandValidator
    : AbstractValidator<CreateReturnedPropertyReceiptCommand>
{
    public CreateReturnedPropertyReceiptCommandValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.AccountabilityId).NotEqual(Guid.Empty);
        RuleFor(x => x.AccountabilityLineIds).NotEmpty()
            .WithMessage("At least one accountability line must be selected.");
        RuleFor(x => x.ReturnedBy).NotNull();
        RuleFor(x => x.ReturnedBy.PrintedName).NotEmpty().MaximumLength(200)
            .When(x => x.ReturnedBy is not null);
        RuleFor(x => x.ReturnedBy.Designation).MaximumLength(200)
            .When(x => x.ReturnedBy is not null);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public sealed class InspectReturnedPropertyReceiptCommandValidator
    : AbstractValidator<InspectReturnedPropertyReceiptCommand>
{
    public InspectReturnedPropertyReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.InspectedBy).NotNull();
        RuleFor(x => x.InspectedBy.PrintedName).NotEmpty().MaximumLength(200)
            .When(x => x.InspectedBy is not null);
        RuleFor(x => x.InspectedBy.Designation).MaximumLength(200)
            .When(x => x.InspectedBy is not null);
        RuleFor(x => x.ItemConditions).NotEmpty()
            .WithMessage("At least one item condition must be recorded.");
        RuleForEach(x => x.ItemConditions).ChildRules(ic =>
        {
            ic.RuleFor(i => i.ItemId).NotEqual(Guid.Empty);
            ic.RuleFor(i => i.Condition).IsInEnum();
        });
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public sealed class AcceptReturnedPropertyReceiptCommandValidator
    : AbstractValidator<AcceptReturnedPropertyReceiptCommand>
{
    public AcceptReturnedPropertyReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.ReceivedBy).NotNull();
        RuleFor(x => x.ReceivedBy.PrintedName).NotEmpty().MaximumLength(200)
            .When(x => x.ReceivedBy is not null);
        RuleFor(x => x.ReceivedBy.Designation).MaximumLength(200)
            .When(x => x.ReceivedBy is not null);
    }
}

public sealed class RejectReturnedPropertyReceiptCommandValidator
    : AbstractValidator<RejectReturnedPropertyReceiptCommand>
{
    public RejectReturnedPropertyReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CancelReturnedPropertyReceiptCommandValidator
    : AbstractValidator<CancelReturnedPropertyReceiptCommand>
{
    public CancelReturnedPropertyReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
