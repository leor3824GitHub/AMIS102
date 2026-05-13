using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryDisposed;

public sealed class MarkRejectedInventoryDisposedCommandValidator : AbstractValidator<MarkRejectedInventoryDisposedCommand>
{
    public MarkRejectedInventoryDisposedCommandValidator()
    {
        RuleFor(x => x.RejectedInventoryId)
            .NotEmpty().WithMessage("Rejected Inventory ID is required");

        RuleFor(x => x.DisposalMethod)
            .NotEmpty().WithMessage("Disposal method is required")
            .MaximumLength(200).WithMessage("Disposal method cannot exceed 200 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("Notes cannot exceed 500 characters");
    }
}

