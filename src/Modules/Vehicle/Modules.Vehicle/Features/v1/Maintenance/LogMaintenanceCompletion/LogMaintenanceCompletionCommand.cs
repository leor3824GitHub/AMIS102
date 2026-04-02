using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;

public sealed record LogMaintenanceCompletionCommand(
    Guid VehicleId,
    Guid? ScheduleId,
    string MaintenanceType,
    DateOnly PerformedDate,
    int? OdometerAtService,
    string? Description,
    decimal? Cost,
    string? PerformedBy,
    string? Notes) : ICommand<LogMaintenanceCompletionResponse>;
