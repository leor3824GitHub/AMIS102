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

        // Asset lines must link to an AssetRegister catalog item (drives IAR/PPERR registration); Supply
        // lines must carry an Expendable StockNo (drives ProductInventory landing). One PR is wholly one
        // category, so the required identifier differs by ProcurementCategory.
        When(x => x.Category == ProcurementCategory.Asset, () =>
            RuleForEach(x => x.LineItems).ChildRules(li =>
                li.RuleFor(x => x.CatalogItemId)
                    .NotNull().NotEqual(Guid.Empty)
                    .WithMessage("Pick a catalog item for every line — use the + button to create one from your description. " +
                                 "The catalog link is required so downstream IAR and PPERR can register the asset.")));

        When(x => x.Category == ProcurementCategory.Supply, () =>
            RuleForEach(x => x.LineItems).ChildRules(li =>
                li.RuleFor(x => x.StockNumber)
                    .NotEmpty()
                    .WithMessage("Pick an Expendable product for every line. The Stock No is required so accepted " +
                                 "stock can be landed into inventory.")));
    }
}

