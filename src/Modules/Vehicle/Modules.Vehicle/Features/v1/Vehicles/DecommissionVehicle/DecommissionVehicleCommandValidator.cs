using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.DecommissionVehicle;

public sealed class DecommissionVehicleCommandValidator : AbstractValidator<DecommissionVehicleCommand>
{
    public DecommissionVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
