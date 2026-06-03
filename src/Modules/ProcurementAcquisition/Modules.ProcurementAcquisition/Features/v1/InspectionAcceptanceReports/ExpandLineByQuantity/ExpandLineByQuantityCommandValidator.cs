using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.ExpandLineByQuantity;

public sealed class ExpandLineByQuantityCommandValidator : AbstractValidator<ExpandLineByQuantityCommand>
{
    public ExpandLineByQuantityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ItemNo).GreaterThan(0);
    }
}
