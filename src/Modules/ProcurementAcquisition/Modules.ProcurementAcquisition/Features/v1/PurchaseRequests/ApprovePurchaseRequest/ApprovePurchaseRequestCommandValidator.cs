using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;

public sealed class ApprovePurchaseRequestCommandValidator : AbstractValidator<ApprovePurchaseRequestCommand>
{
    public ApprovePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApprovedById).NotEmpty().WithMessage("Approver employee is required.");
    }
}

