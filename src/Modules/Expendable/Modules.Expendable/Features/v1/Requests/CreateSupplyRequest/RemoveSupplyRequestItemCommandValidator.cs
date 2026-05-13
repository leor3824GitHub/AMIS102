using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public sealed class RemoveSupplyRequestItemCommandValidator : AbstractValidator<RemoveSupplyRequestItemCommand>
{
    public RemoveSupplyRequestItemCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");
    }
}

