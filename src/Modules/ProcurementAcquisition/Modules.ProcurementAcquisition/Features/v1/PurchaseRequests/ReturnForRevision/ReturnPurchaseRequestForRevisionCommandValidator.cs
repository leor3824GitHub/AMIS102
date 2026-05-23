using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ReturnForRevision;

public sealed class ReturnPurchaseRequestForRevisionCommandValidator : AbstractValidator<ReturnPurchaseRequestForRevisionCommand>
{
    public ReturnPurchaseRequestForRevisionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReturnedByName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("A reason is required when returning a PR for revision.");
    }
}
