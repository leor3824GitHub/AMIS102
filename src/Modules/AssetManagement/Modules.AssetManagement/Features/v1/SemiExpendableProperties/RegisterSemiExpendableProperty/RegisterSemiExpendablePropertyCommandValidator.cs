using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.RegisterSemiExpendableProperty;

public sealed class RegisterSemiExpendablePropertyCommandValidator : AbstractValidator<RegisterSemiExpendablePropertyCommand>
{
    public RegisterSemiExpendablePropertyCommandValidator()
    {
        RuleFor(x => x.PropertyNo)
            .NotEmpty().WithMessage("Property No. is required.")
            .MaximumLength(32).WithMessage("Property No. must not exceed 32 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid AssetCategory value.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item is required.");

        RuleFor(x => x.SerialNo)
            .MaximumLength(100).WithMessage("Serial No. must not exceed 100 characters.");

        RuleFor(x => x.AcquisitionDate)
            .NotEmpty().WithMessage("Acquisition date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Acquisition date cannot be in the future.");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("Unit cost must be greater than 0.")
            .LessThan(50_000m).WithMessage("Items costing ₱50,000 or more must be recorded as Fixed Assets (PPE), not as semi-expendable property.");

        RuleFor(x => x.FundCluster)
            .MaximumLength(50).WithMessage("Fund cluster must not exceed 50 characters.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500).WithMessage("Remarks must not exceed 500 characters.");
    }
}
