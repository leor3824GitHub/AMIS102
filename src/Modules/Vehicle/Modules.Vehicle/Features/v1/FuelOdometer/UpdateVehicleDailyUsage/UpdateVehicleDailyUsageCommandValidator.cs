using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.FuelOdometer.UpdateVehicleDailyUsage;

public sealed class UpdateVehicleDailyUsageCommandValidator : AbstractValidator<UpdateVehicleDailyUsageCommand>
{
    public UpdateVehicleDailyUsageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.OdometerStart).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OdometerEnd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OdometerEnd)
            .GreaterThanOrEqualTo(x => x.OdometerStart)
            .WithMessage("Odometer end cannot be less than odometer start.");
        RuleFor(x => x.FuelLiters).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FuelCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Destination).MaximumLength(250).When(x => x.Destination is not null);
        RuleFor(x => x.Remarks).MaximumLength(1000).When(x => x.Remarks is not null);
    }
}

