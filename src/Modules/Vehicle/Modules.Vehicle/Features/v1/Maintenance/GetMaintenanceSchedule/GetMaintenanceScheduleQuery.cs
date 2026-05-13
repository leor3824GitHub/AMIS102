using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;

public sealed record GetMaintenanceScheduleQuery(Guid ScheduleId) : IQuery<MaintenanceScheduleDto>;

