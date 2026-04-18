using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;

public sealed class RegisterTangibleItemCommandValidator : AbstractValidator<RegisterTangibleItemCommand>
{
    public RegisterTangibleItemCommandValidator()
    {
        RuleFor(x => x.PropertyNo)
            .NotEmpty().WithMessage("Property No. is required.")
            .MaximumLength(32).WithMessage("Property No. must not exceed 32 characters.");

        RuleFor(x => x.PropertyClass)
            .NotEmpty().WithMessage("Property Class is required.")
            .MaximumLength(20);

        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category Code is required.")
            .MaximumLength(20);

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item is required.");

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
