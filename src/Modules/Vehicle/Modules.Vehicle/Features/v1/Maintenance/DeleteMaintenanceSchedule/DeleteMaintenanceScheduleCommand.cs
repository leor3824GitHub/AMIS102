using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;

public sealed record DeleteMaintenanceScheduleCommand(Guid ScheduleId) : ICommand<Unit>;
