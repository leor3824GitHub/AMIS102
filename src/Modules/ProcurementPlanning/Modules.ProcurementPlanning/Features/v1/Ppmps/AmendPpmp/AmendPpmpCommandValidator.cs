using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.AmendPpmp;

public sealed class AmendPpmpCommandValidator : AbstractValidator<AmendPpmpCommand>
{
    public AmendPpmpCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AmendmentReason).NotEmpty().MaximumLength(1000);
    }
}
