using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;

public sealed record GetMaintenanceScheduleQuery(Guid ScheduleId) : IQuery<MaintenanceScheduleDto>;
