using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CancelPurchaseRequest;

public sealed class CancelPurchaseRequestCommandValidator : AbstractValidator<CancelPurchaseRequestCommand>
{
    public CancelPurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        // Reason is optional; cap it to the stored CancellationReason length when supplied.
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
