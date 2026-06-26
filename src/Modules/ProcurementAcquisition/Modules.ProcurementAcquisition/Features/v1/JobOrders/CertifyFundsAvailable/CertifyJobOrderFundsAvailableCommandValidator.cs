using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CertifyFundsAvailable;

public sealed class CertifyJobOrderFundsAvailableCommandValidator : AbstractValidator<CertifyJobOrderFundsAvailableCommand>
{
    public CertifyJobOrderFundsAvailableCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CertifiedByName)
            .NotEmpty().MaximumLength(200)
            .WithMessage("Accountant name (Funds Available signatory) is required.");
        RuleFor(x => x.OursBursNumber).MaximumLength(100);
        RuleFor(x => x.FundCluster).MaximumLength(100);
    }
}
