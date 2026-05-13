using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryReturned;

public sealed class MarkRejectedInventoryReturnedCommandValidator : AbstractValidator<MarkRejectedInventoryReturnedCommand>
{
    public MarkRejectedInventoryReturnedCommandValidator()
    {
        RuleFor(x => x.RejectedInventoryId)
            .NotEmpty().WithMessage("Rejected Inventory ID is required");

        RuleFor(x => x.QuantityReturned)
            .GreaterThan(0).When(x => x.QuantityReturned.HasValue)
            .WithMessage("Quantity returned must be greater than 0");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("Notes cannot exceed 500 characters");
    }
}

