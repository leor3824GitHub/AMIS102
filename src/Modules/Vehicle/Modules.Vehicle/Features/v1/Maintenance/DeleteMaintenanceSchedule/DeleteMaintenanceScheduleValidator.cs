using FluentValidation;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;

public sealed class DeleteMaintenanceScheduleValidator : AbstractValidator<DeleteMaintenanceScheduleCommand>
{
    public DeleteMaintenanceScheduleValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
    }
}