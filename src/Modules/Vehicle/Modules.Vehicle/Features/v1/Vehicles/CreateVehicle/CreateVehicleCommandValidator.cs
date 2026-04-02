using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Domain.Vehicles;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;

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
    }
}
