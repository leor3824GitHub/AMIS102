using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse;

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


