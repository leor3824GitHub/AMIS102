using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;

public sealed class DeleteVehicleCommandValidator : AbstractValidator<DeleteVehicleCommand>
{
    public DeleteVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
