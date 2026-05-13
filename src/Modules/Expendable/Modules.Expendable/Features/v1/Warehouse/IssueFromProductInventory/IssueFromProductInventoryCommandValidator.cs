using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.IssueFromProductInventory;

public sealed class IssueFromProductInventoryCommandValidator : AbstractValidator<IssueFromProductInventoryCommand>
{
    public IssueFromProductInventoryCommandValidator()
    {
        RuleFor(x => x.ProductInventoryId)
            .NotEmpty().WithMessage("Product Inventory ID is required");

        RuleFor(x => x.QuantityToIssue)
            .GreaterThan(0).WithMessage("Quantity to issue must be greater than 0");
    }
}

