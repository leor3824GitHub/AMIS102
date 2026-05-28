using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CertifyFundsAvailable;

public sealed class CertifyPurchaseOrderFundsAvailableCommandValidator : AbstractValidator<CertifyPurchaseOrderFundsAvailableCommand>
{
    public CertifyPurchaseOrderFundsAvailableCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CertifiedByName)
            .NotEmpty().MaximumLength(200)
            .WithMessage("Accountant name (Funds Available signatory) is required.");
        RuleFor(x => x.UacsByLine)
            .NotEmpty().WithMessage("At least one line UACS assignment is required.");
        RuleForEach(x => x.UacsByLine).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemNo).GreaterThan(0);
            line.RuleFor(l => l.UacsObjectCode)
                .NotEmpty().MaximumLength(64)
                .WithMessage("UACS Object Code is required for every line.");
        });
        RuleFor(x => x.OursBursNumber).MaximumLength(100);
        RuleFor(x => x.FundCluster).MaximumLength(100);
    }
}
