using FluentValidation;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;

public sealed class ApprovePurchaseRequestCommandValidator : AbstractValidator<ApprovePurchaseRequestCommand>
{
    public ApprovePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApprovedById).NotEmpty().WithMessage("Approver employee is required.");
    }
}
