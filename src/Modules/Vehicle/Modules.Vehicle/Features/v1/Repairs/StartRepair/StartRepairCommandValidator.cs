using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.StartRepair;

public sealed class StartRepairCommandValidator : AbstractValidator<StartRepairCommand>
{
    public StartRepairCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}