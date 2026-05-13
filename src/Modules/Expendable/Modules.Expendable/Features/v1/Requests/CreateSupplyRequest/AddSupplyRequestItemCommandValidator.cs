using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public sealed class AddSupplyRequestItemCommandValidator : AbstractValidator<AddSupplyRequestItemCommand>
{
    public AddSupplyRequestItemCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters");
    }
}

