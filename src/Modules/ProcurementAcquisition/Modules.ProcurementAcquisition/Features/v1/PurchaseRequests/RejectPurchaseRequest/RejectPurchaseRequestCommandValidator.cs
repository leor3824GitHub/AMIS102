using FluentValidation;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.RejectPurchaseRequest;

public sealed class RejectPurchaseRequestCommandValidator : AbstractValidator<RejectPurchaseRequestCommand>
{
    public RejectPurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
