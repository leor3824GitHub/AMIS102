using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;

public sealed class CreateMaintenanceScheduleValidator : AbstractValidator<CreateMaintenanceScheduleCommand>
{
    public CreateMaintenanceScheduleValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("Vehicle ID is required");
        RuleFor(x => x.MaintenanceType).NotEmpty().MaximumLength(100).WithMessage("Maintenance type is required and must be 100 characters or less");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x).Custom((cmd, context) =>
        {
            if (!cmd.IntervalDays.HasValue && !cmd.IntervalMileage.HasValue)
            {
                context.AddFailure("Either IntervalDays or IntervalMileage must be specified");
            }
        });
    }
}
