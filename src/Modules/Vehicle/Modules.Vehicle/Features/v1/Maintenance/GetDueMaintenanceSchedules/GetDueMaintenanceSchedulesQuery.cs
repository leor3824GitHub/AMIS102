using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetDueMaintenanceSchedules;

public sealed record GetDueMaintenanceSchedulesQuery(
    Guid? VehicleId,
    int DaysAhead) : IQuery<List<MaintenanceScheduleDto>>;
