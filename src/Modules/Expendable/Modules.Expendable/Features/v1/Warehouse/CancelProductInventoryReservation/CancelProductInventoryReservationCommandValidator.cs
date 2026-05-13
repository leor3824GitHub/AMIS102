using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.CancelProductInventoryReservation;

public sealed class CancelProductInventoryReservationCommandValidator : AbstractValidator<CancelProductInventoryReservationCommand>
{
    public CancelProductInventoryReservationCommandValidator()
    {
        RuleFor(x => x.ProductInventoryId)
            .NotEmpty().WithMessage("Product Inventory ID is required");

        RuleFor(x => x.QuantityToRelease)
            .GreaterThan(0).WithMessage("Quantity to release must be greater than 0");
    }
}

