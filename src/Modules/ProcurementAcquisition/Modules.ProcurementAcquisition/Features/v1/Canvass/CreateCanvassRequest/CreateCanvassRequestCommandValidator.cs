using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;

public sealed class CreateCanvassRequestCommandValidator : AbstractValidator<CreateCanvassRequestCommand>
{
    public CreateCanvassRequestCommandValidator()
    {
        RuleFor(x => x.PurchaseRequestId).NotEmpty();
        RuleFor(x => x.ReturnDeadline).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Return deadline must be a future date.");
    }
}

