using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;

public sealed record SearchMaintenanceSchedulesQuery(
    string? MaintenanceType,
    Guid? VehicleId,
    bool? IsActive) : IQuery<List<MaintenanceScheduleDto>>;

