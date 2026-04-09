using FluentValidation;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SubmitPurchaseRequest;

public sealed class SubmitPurchaseRequestCommandValidator : AbstractValidator<SubmitPurchaseRequestCommand>
{
    public SubmitPurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
