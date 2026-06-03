using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AcceptInspectionAcceptanceReport;

public sealed class AcceptInspectionAcceptanceReportCommandValidator : AbstractValidator<AcceptInspectionAcceptanceReportCommand>
{
    public AcceptInspectionAcceptanceReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
