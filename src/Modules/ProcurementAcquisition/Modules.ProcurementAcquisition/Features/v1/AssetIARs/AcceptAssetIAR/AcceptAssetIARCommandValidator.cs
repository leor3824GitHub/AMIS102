using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.AcceptAssetIAR;

public sealed class AcceptAssetIARCommandValidator : AbstractValidator<AcceptAssetIARCommand>
{
    public AcceptAssetIARCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
