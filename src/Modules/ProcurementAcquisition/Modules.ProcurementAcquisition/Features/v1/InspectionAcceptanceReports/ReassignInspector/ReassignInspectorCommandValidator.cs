using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using FluentValidation;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.ReassignInspector;

public sealed class ReassignInspectorCommandValidator : AbstractValidator<ReassignInspectorCommand>
{
    public ReassignInspectorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewInspectorId).NotEmpty().WithMessage("New inspector is required.");
    }
}
