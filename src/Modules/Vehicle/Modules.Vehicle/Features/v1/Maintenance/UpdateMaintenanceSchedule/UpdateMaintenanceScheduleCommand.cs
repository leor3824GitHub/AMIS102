using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;

public sealed record UpdateMaintenanceScheduleCommand(
    Guid ScheduleId,
    string MaintenanceType,
    string? Description,
    int? IntervalDays,
    int? IntervalMileage,
    DateOnly? DueDate,
    int? DueMileage) : ICommand<MaintenanceScheduleDto>;
