using FluentValidation;
using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;

public sealed class CreateSMIRCommandValidator : AbstractValidator<CreateSMIRCommand>
{
    public CreateSMIRCommandValidator()
    {
        RuleFor(x => x.SMIRNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.FundCluster).MaximumLength(50);
        RuleFor(x => x.IssuanceType).IsInEnum();

        RuleFor(x => x.TransferredToTenantId)
            .NotEmpty()
            .MaximumLength(64)
            .When(x => x.IssuanceType == SMIRIssuanceType.Transfer)
            .WithMessage("TransferredToTenantId is required when IssuanceType is Transfer.");

        RuleFor(x => x.TransferredToOfficerName).MaximumLength(200);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreateSMIRItemRequestValidator());
    }
}

internal sealed class CreateSMIRItemRequestValidator : AbstractValidator<CreateSMIRItemRequest>
{
    public CreateSMIRItemRequestValidator()
    {
        RuleFor(x => x.SemiExpendablePropertyId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
