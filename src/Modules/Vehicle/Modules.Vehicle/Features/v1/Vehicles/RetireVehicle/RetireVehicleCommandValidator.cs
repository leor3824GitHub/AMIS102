using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.RetireVehicle;

public sealed class RetireVehicleCommandValidator : AbstractValidator<RetireVehicleCommand>
{
    public RetireVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
