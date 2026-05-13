using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.CreateSupplier;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(160).WithMessage("Name must not exceed 160 characters.");

        RuleFor(x => x.TinNo)
            .MaximumLength(32).WithMessage("TIN No. must not exceed 32 characters.");

        RuleFor(x => x.BusinessTaxType)
            .NotEmpty().WithMessage("Business tax type is required.")
            .Must(x => string.Equals(x, "VAT", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "NON-VAT", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Business tax type must be either VAT or NON-VAT.");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters.");

        RuleFor(x => x.ContactPerson)
            .MaximumLength(160).WithMessage("Contact person must not exceed 160 characters.");

        RuleFor(x => x.Email)
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(400).WithMessage("Address must not exceed 400 characters.");
    }
}

