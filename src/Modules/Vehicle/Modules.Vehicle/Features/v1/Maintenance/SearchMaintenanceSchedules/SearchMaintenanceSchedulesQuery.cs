using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;

public sealed record SearchMaintenanceSchedulesQuery(
    string? MaintenanceType,
    Guid? VehicleId,
    bool? IsActive) : IQuery<List<MaintenanceScheduleDto>>;
