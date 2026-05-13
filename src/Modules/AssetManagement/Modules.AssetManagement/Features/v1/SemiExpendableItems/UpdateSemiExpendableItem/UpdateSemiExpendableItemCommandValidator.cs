using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;

public sealed class UpdatePropertyItemCatalogCommandValidator : AbstractValidator<UpdatePropertyItemCatalogCommand>
{
    public UpdatePropertyItemCatalogCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.UACSObjectCode)
            .MaximumLength(32).WithMessage("UACS Object Code must not exceed 32 characters.");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty().WithMessage("Unit of measure is required.")
            .MaximumLength(50).WithMessage("Unit of measure must not exceed 50 characters.");

        RuleFor(x => x.EstimatedUsefulLifeYears)
            .GreaterThan(0).WithMessage("Estimated useful life must be greater than 0.")
            .When(x => x.EstimatedUsefulLifeYears.HasValue);
    }
}

