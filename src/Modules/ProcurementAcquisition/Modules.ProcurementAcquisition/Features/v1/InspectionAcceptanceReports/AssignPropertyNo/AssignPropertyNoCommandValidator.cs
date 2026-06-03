using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using FluentValidation;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AssignPropertyNo;

public sealed class AssignPropertyNoCommandValidator : AbstractValidator<AssignPropertyNoCommand>
{
    public AssignPropertyNoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ItemNo).GreaterThan(0);
        RuleFor(x => x.PropertyNo).NotEmpty().MaximumLength(64);
    }
}
