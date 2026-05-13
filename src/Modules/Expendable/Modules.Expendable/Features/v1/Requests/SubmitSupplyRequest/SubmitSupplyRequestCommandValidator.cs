using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.SubmitSupplyRequest;

public sealed class SubmitSupplyRequestCommandValidator : AbstractValidator<SubmitSupplyRequestCommand>
{
    public SubmitSupplyRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Request ID is required");
    }
}

