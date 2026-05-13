using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;

public sealed record DeleteMaintenanceScheduleCommand(Guid ScheduleId) : ICommand<Unit>;

