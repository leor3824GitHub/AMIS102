using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;

public sealed class AwardCanvassCommandValidator : AbstractValidator<AwardCanvassCommand>
{
    public AwardCanvassCommandValidator()
    {
        RuleFor(x => x.CanvassRequestId).NotEmpty();
        RuleFor(x => x.AwardedQuotationId).NotEmpty();
    }
}

