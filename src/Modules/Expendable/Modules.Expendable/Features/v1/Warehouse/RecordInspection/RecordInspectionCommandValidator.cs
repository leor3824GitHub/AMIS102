using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.RecordInspection;

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

