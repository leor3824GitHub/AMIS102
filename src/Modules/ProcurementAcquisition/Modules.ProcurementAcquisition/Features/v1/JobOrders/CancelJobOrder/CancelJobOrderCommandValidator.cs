using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CancelJobOrder;

public sealed class CancelJobOrderCommandValidator : AbstractValidator<CancelJobOrderCommand>
{
    public CancelJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
