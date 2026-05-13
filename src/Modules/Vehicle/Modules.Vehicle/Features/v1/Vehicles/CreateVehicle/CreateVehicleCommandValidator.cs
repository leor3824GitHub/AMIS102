using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Domain.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.PlateNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Type).NotEmpty()
            .Must(t => Enum.TryParse<VehicleType>(t, ignoreCase: true, out _))
            .WithMessage("Invalid vehicle type. Valid values: " + string.Join(", ", Enum.GetNames<VehicleType>()));
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
        RuleFor(x => x.MotorNumber).MaximumLength(100).When(x => x.MotorNumber != null);
        RuleFor(x => x.ChassisNumber).MaximumLength(100).When(x => x.ChassisNumber != null);
        RuleFor(x => x.NumberOfCylinders).GreaterThan(0).When(x => x.NumberOfCylinders != null);
        RuleFor(x => x.EngineDisplacementCC).GreaterThan(0).When(x => x.EngineDisplacementCC != null);
        RuleFor(x => x.FuelType).MaximumLength(50).When(x => x.FuelType != null);
        RuleFor(x => x.VehicleUse).MaximumLength(100).When(x => x.VehicleUse != null);
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0).When(x => x.AcquisitionCost != null);
    }
}

