using FluentValidation;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;

public sealed class DeactivateMaintenanceScheduleValidator : AbstractValidator<DeactivateMaintenanceScheduleCommand>
{
    public DeactivateMaintenanceScheduleValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
    }
}