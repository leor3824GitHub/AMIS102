using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;

public sealed class ReturnPpmpCommandValidator : AbstractValidator<ReturnPpmpCommand>
{
    public ReturnPpmpCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().MaximumLength(1000);
    }
}

