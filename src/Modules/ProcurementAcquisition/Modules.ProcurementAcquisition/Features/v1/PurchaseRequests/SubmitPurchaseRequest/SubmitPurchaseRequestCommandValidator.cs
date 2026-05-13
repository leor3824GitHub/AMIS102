using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SubmitPurchaseRequest;

public sealed class SubmitPurchaseRequestCommandValidator : AbstractValidator<SubmitPurchaseRequestCommand>
{
    public SubmitPurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

