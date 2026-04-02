using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;

public sealed class DeleteVehicleCommandValidator : AbstractValidator<DeleteVehicleCommand>
{
    public DeleteVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}