namespace FSH.Modules.Vehicle.Contracts.v1.Maintenance;

public sealed record MaintenanceScheduleDto(
    Guid Id,
    Guid VehicleId,
    string MaintenanceType,
    string? Description,
    int? IntervalDays,
    int? IntervalMileage,
    DateOnly? DueDate,
    int? DueMileage,
    DateOnly? LastDoneDate,
    int? LastDoneMileage,
    bool IsActive);

public sealed record CreateMaintenanceScheduleRequest(
    Guid VehicleId,
    string MaintenanceType,
    string? Description,
    int? IntervalDays,
    int? IntervalMileage,
    DateOnly? InitialDueDate,
    int? InitialDueMileage);

public sealed record CreateMaintenanceScheduleResponse(Guid Id);

public sealed record UpdateMaintenanceScheduleRequest(
    string MaintenanceType,
    string? Description,
    int? IntervalDays,
    int? IntervalMileage,
    DateOnly? DueDate,
    int? DueMileage);

public sealed record MaintenanceScheduleSearchRequest(
    string? MaintenanceType,
    Guid? VehicleId,
    bool? IsActive);
