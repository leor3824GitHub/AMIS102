using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SubmitJobOrder;

public sealed class SubmitJobOrderCommandValidator : AbstractValidator<SubmitJobOrderCommand>
{
    public SubmitJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
