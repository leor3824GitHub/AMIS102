using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;

public sealed class ReactivateVehicleCommandValidator : AbstractValidator<ReactivateVehicleCommand>
{
    public ReactivateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}