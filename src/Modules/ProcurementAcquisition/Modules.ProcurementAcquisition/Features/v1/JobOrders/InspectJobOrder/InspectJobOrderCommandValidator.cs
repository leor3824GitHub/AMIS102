using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.InspectJobOrder;

public sealed class InspectJobOrderCommandValidator : AbstractValidator<InspectJobOrderCommand>
{
    public InspectJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceNo).MaximumLength(64);
        RuleFor(x => x.Findings).MaximumLength(1000);
    }
}
