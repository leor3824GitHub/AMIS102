using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;

public sealed class ReactivateVehicleCommandValidator : AbstractValidator<ReactivateVehicleCommand>
{
    public ReactivateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
