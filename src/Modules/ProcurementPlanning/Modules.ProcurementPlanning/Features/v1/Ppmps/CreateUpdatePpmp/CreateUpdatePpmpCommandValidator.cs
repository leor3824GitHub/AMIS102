using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;

public sealed class CreateUpdatePpmpCommandValidator : AbstractValidator<CreateUpdatePpmpCommand>
{
    public CreateUpdatePpmpCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UpdateReason).NotEmpty().MaximumLength(1000);
    }
}
