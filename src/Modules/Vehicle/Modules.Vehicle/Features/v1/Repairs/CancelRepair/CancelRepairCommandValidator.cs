using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.CancelRepair;

public sealed class CancelRepairCommandValidator : AbstractValidator<CancelRepairCommand>
{
    public CancelRepairCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
