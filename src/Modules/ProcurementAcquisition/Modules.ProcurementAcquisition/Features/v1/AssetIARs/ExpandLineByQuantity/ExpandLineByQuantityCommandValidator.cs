using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.ExpandLineByQuantity;

public sealed class ExpandLineByQuantityCommandValidator : AbstractValidator<ExpandLineByQuantityCommand>
{
    public ExpandLineByQuantityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ItemNo).GreaterThan(0);
    }
}
