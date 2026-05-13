using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;

public sealed record CreateMaintenanceScheduleCommand(
    Guid VehicleId,
    string MaintenanceType,
    string? Description,
    int? IntervalDays,
    int? IntervalMileage,
    DateOnly? InitialDueDate,
    int? InitialDueMileage) : ICommand<CreateMaintenanceScheduleResponse>;

