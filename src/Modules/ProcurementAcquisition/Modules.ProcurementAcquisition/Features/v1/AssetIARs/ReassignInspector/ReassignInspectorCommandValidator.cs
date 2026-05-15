using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using FluentValidation;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.ReassignInspector;

public sealed class ReassignInspectorCommandValidator : AbstractValidator<ReassignInspectorCommand>
{
    public ReassignInspectorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewInspectorId).NotEmpty().WithMessage("New inspector is required.");
    }
}
