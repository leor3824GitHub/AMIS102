namespace AMIS.Modules.Vehicle.Contracts.v1.Maintenance;

public sealed record MaintenanceLogDto(
    Guid Id,
    Guid VehicleId,
    Guid? ScheduleId,
    string MaintenanceType,
    DateOnly PerformedDate,
    int? OdometerAtService,
    string? Description,
    decimal? Cost,
    string? PerformedBy,
    string? Notes);

public sealed record LogMaintenanceCompletionRequest(
    Guid VehicleId,
    Guid? ScheduleId,
    string MaintenanceType,
    DateOnly PerformedDate,
    int? OdometerAtService,
    string? Description,
    decimal? Cost,
    string? PerformedBy,
    string? Notes);

public sealed record LogMaintenanceCompletionResponse(Guid Id);

public sealed record UpdateMaintenanceLogRequest(
    string MaintenanceType,
    DateOnly PerformedDate,
    int? OdometerAtService,
    string? Description,
    decimal? Cost,
    string? PerformedBy,
    string? Notes);

public sealed record MaintenanceLogSearchRequest(
    string? MaintenanceType,
    Guid? VehicleId,
    Guid? ScheduleId,
    DateOnly? PerformedDateFrom,
    DateOnly? PerformedDateTo);

public sealed record DueMaintenanceScheduleSearchRequest(
    Guid? VehicleId,
    int DaysAhead = 30);

