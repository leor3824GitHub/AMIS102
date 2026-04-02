using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;

public sealed record UpdateMaintenanceLogCommand(
    Guid LogId,
    string MaintenanceType,
    DateOnly PerformedDate,
    int? OdometerAtService,
    string? Description,
    decimal? Cost,
    string? PerformedBy,
    string? Notes) : ICommand<MaintenanceLogDto>;
