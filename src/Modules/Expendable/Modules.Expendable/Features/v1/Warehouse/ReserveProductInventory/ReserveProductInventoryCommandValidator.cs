using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.ReserveProductInventory;

public sealed class ReserveProductInventoryCommandValidator : AbstractValidator<ReserveProductInventoryCommand>
{
    public ReserveProductInventoryCommandValidator()
    {
        RuleFor(x => x.ProductInventoryId)
            .NotEmpty().WithMessage("Product Inventory ID is required");

        RuleFor(x => x.QuantityToReserve)
            .GreaterThan(0).WithMessage("Quantity to reserve must be greater than 0");
    }
}

