using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.UpdateAssetIAR;

public sealed class UpdateAssetIARCommandValidator : AbstractValidator<UpdateAssetIARCommand>
{
    public UpdateAssetIARCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InspectedById).NotEmpty().WithMessage("Inspector is required.");
        RuleFor(x => x.ReceivedById).NotEmpty().WithMessage("Receiver is required.");
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitCost).GreaterThan(0);
            li.RuleFor(x => x.StockPropertyNo).MaximumLength(64)
                .When(x => !string.IsNullOrWhiteSpace(x.StockPropertyNo));
        });
    }
}
