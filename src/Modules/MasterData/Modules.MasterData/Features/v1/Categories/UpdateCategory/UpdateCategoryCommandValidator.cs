using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(160).WithMessage("Name must not exceed 160 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters.");
    }
}

