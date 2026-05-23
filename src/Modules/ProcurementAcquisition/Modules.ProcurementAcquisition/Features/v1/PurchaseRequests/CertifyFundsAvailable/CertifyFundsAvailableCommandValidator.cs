using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CertifyFundsAvailable;

public sealed class CertifyFundsAvailableCommandValidator : AbstractValidator<CertifyFundsAvailableCommand>
{
    public CertifyFundsAvailableCommandValidator()
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
        RuleFor(x => x.AlobsNumber).MaximumLength(64);
    }
}
