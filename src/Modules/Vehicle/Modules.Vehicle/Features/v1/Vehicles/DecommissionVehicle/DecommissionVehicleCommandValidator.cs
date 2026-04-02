using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.DecommissionVehicle;

public sealed class DecommissionVehicleCommandValidator : AbstractValidator<DecommissionVehicleCommand>
{
    public DecommissionVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}