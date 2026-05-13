using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.UpdateTangibleItem;

public sealed class UpdateTangibleItemCommandValidator : AbstractValidator<UpdateTangibleItemCommand>
{
    public UpdateTangibleItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tangible item ID is required.");

        RuleFor(x => x.AcquisitionDate)
            .NotEmpty().WithMessage("Acquisition date is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(9999).WithMessage("Quantity must not exceed 9999.");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("Unit cost must be greater than 0.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500).WithMessage("Remarks must not exceed 500 characters.");
    }
}

