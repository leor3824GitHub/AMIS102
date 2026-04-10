using FluentValidation;
using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;

public sealed class CreateSMRRCommandValidator : AbstractValidator<CreateSMRRCommand>
{
    public CreateSMRRCommandValidator()
    {
        RuleFor(x => x.SMRRNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.ReceivedFrom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.OtherReceiptType)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.ReceiptType == ReceiptType.Others)
            .WithMessage("OtherReceiptType is required when ReceiptType is Others.");
        RuleFor(x => x.FundCluster).MaximumLength(50);
        RuleFor(x => x.ReceivedBy).MaximumLength(200);
        RuleFor(x => x.NotedBy).MaximumLength(200);

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreateSMRRItemRequestValidator());
    }
}

internal sealed class CreateSMRRItemRequestValidator : AbstractValidator<CreateSMRRItemRequest>
{
    public CreateSMRRItemRequestValidator()
    {
        RuleFor(x => x.SemiExpendableItemId).NotEmpty();
        RuleFor(x => x.AcquisitionDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThan(0).LessThan(50_000m)
            .WithMessage("Unit cost must be less than ₱50,000. Items at or above the capitalization threshold must be registered as Fixed Assets.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}
