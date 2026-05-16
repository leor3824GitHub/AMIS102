using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;

public sealed class UpdatePurchaseRequestCommandValidator : AbstractValidator<UpdatePurchaseRequestCommand>
{
    public UpdatePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("Department is required.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestedByName).NotEmpty().MaximumLength(200).WithMessage("Requested by name is required.");
        RuleFor(x => x.Justification)
            .NotEmpty().When(x => x.PrType == PrType.Unplanned)
            .WithMessage("Justification is required for Unplanned purchase requests.");
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitOfIssue).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.EstimatedUnitCost).GreaterThan(0);
        });
    }
}

