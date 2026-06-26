using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.IssueJobOrder;

public sealed class IssueJobOrderCommandValidator : AbstractValidator<IssueJobOrderCommand>
{
    public IssueJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
