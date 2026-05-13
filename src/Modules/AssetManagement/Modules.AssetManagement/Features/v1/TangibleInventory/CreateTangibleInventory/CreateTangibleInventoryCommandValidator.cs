using FluentValidation;
using AMIS.Modules.AssetManagement.Domain;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;

public sealed class CreateTangibleInventoryCommandValidator : AbstractValidator<CreateTangibleInventoryCommand>
{
    public CreateTangibleInventoryCommandValidator()
    {
        RuleFor(x => x.ReportNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.OtherReceiptType)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.ReceiptType == ReceiptType.Others)
            .WithMessage("OtherReceiptType is required when ReceiptType is Others.");
        RuleFor(x => x.FundCluster).MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one tangible item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreateTangibleInventoryItemRequestValidator());
    }
}

internal sealed class CreateTangibleInventoryItemRequestValidator : AbstractValidator<CreateTangibleInventoryItemRequest>
{
    public CreateTangibleInventoryItemRequestValidator()
    {
        RuleFor(x => x.TangibleItemId).NotEmpty().WithMessage("TangibleItemId is required.");
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}

