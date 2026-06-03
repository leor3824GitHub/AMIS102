using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CancelInspectionAcceptanceReport;

public sealed class CancelInspectionAcceptanceReportCommandValidator : AbstractValidator<CancelInspectionAcceptanceReportCommand>
{
    public CancelInspectionAcceptanceReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
