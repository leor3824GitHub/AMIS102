using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.CancelRepair;

public sealed class CancelRepairCommandValidator : AbstractValidator<CancelRepairCommand>
{
    public CancelRepairCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}