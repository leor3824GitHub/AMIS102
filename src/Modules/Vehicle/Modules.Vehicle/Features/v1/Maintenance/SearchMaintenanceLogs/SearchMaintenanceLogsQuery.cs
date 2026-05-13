using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;

public sealed record SearchMaintenanceLogsQuery(
    string? MaintenanceType,
    Guid? VehicleId,
    Guid? ScheduleId,
    DateOnly? PerformedDateFrom,
    DateOnly? PerformedDateTo) : IQuery<List<MaintenanceLogDto>>;

