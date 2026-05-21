using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SubmitForInspection;

public sealed class SubmitIARForInspectionCommandValidator : AbstractValidator<SubmitIARForInspectionCommand>
{
    public SubmitIARForInspectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
