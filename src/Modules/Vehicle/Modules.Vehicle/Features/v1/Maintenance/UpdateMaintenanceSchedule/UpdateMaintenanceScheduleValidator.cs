using FluentValidation;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;

public sealed class UpdateMaintenanceScheduleValidator : AbstractValidator<UpdateMaintenanceScheduleCommand>
{
    public UpdateMaintenanceScheduleValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.MaintenanceType).NotEmpty().MaximumLength(100);
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

