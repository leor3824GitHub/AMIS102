using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SubmitForInspection;

public sealed class SubmitIARForInspectionCommandValidator : AbstractValidator<SubmitIARForInspectionCommand>
{
    public SubmitIARForInspectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
