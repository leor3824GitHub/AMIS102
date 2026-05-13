using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.Suppliers.DeleteSupplier;

public sealed class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Supplier id is required");
    }
}

