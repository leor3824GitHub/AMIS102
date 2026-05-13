using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse;

public sealed class RecordInspectionCommandValidator : AbstractValidator<RecordInspectionCommand>
{
    public RecordInspectionCommandValidator()
    {
        RuleFor(x => x.PurchaseId)
            .NotEmpty().WithMessage("Purchase ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.QuantityAccepted)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity accepted must be >= 0");

        RuleFor(x => x.QuantityRejected)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity rejected must be >= 0");

        RuleFor(x => x)
            .Must(x => x.QuantityAccepted + x.QuantityRejected > 0)
            .WithMessage("Either quantity accepted or rejected must be greater than 0");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().When(x => x.QuantityRejected > 0)
            .WithMessage("Rejection reason is required when items are rejected");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes != null)
            .WithMessage("Notes cannot exceed 1000 characters");
    }
}

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


