using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CancelAssetIAR;

public sealed class CancelAssetIARCommandValidator : AbstractValidator<CancelAssetIARCommand>
{
    public CancelAssetIARCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
