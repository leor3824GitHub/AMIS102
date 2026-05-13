using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;

public sealed record DeactivateMaintenanceScheduleCommand(Guid ScheduleId) : ICommand<Unit>;

