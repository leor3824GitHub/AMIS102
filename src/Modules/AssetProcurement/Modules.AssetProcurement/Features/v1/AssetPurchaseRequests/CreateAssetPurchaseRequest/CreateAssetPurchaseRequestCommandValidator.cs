using FluentValidation;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;

public sealed class CreateAssetPurchaseRequestCommandValidator : AbstractValidator<CreateAssetPurchaseRequestCommand>
{
    public CreateAssetPurchaseRequestCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("Department is required.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestedById).NotEmpty().WithMessage("Requesting employee is required.");
        RuleFor(x => x.Justification)
            .NotEmpty().When(x => x.PrType == AssetPrType.Unplanned)
            .WithMessage("Justification is required for Unplanned purchase requests.");
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.EstimatedUnitCost).GreaterThan(0);
        });
    }
}
