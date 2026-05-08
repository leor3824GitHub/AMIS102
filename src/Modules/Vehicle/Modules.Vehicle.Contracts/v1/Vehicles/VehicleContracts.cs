using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Vehicle.Contracts.v1.Vehicles;

public static class VehicleStatusValues
{
    public const string Active = "Active";
    public const string UnderRepair = "UnderRepair";
    public const string Retired = "Retired";
    public const string Decommissioned = "Decommissioned";
}

public record VehicleDto(
    Guid Id,
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    string Type,
    string Status,
    int Odometer,
    Guid? AssignedDepartmentId,
    string? AssignedDepartment,
    Guid? AssignedDriverId,
    string? AssignedDriver,
    string? AccountableOfficerTitle,
    string? Notes,
    // Technical specifications
    string? MotorNumber,
    string? ChassisNumber,
    int? NumberOfCylinders,
    int? EngineDisplacementCC,
    string? FuelType,
    string? VehicleUse,
    decimal? AcquisitionCost,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

public record CreateVehicleCommand(
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    string Type,
    int Odometer = 0,
    string? Notes = null,
    string? MotorNumber = null,
    string? ChassisNumber = null,
    int? NumberOfCylinders = null,
    int? EngineDisplacementCC = null,
    string? FuelType = null,
    string? VehicleUse = null,
    decimal? AcquisitionCost = null) : ICommand<VehicleDto>;

public record UpdateVehicleCommand(
    Guid Id,
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    string Type,
    string? Notes,
    string? MotorNumber = null,
    string? ChassisNumber = null,
    int? NumberOfCylinders = null,
    int? EngineDisplacementCC = null,
    string? FuelType = null,
    string? VehicleUse = null,
    decimal? AcquisitionCost = null) : ICommand<VehicleDto>;

public record AssignVehicleCommand(
    Guid Id,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DriverId,
    string? DriverName,
    string? AccountableOfficerTitle = null) : ICommand<Unit>;

public record UpdateOdometerCommand(Guid Id, int Reading) : ICommand<Unit>;

public record RetireVehicleCommand(Guid Id) : ICommand<Unit>;

public record DecommissionVehicleCommand(Guid Id) : ICommand<Unit>;

public record ReactivateVehicleCommand(Guid Id) : ICommand<Unit>;

public record DeleteVehicleCommand(Guid Id) : ICommand<Unit>;

public record GetVehicleQuery(Guid Id) : IQuery<VehicleDto?>;

public sealed class SearchVehiclesQuery : IPagedQuery, IQuery<PagedResponse<VehicleDto>>
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public Guid? AssignedDepartmentId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

// ============= MOTOR VEHICLE INVENTORY REPORT =============

/// <summary>Inventory of Motor Vehicles report — all vehicles with full specifications and accountable officers</summary>
public sealed record GetMotorVehicleInventoryQuery : IQuery<List<MotorVehicleInventoryItemDto>>
{
    public string? Status { get; init; }  // optional filter: Active, Retired, etc.
}

public sealed class GenerateVehicleInventoryPdfCommand : ICommand<byte[]>
{
    public string? Status { get; set; }
    public DateTime? AsOfDate { get; set; }
}

public record VehicleDailyUsageDto(
    Guid Id,
    Guid VehicleId,
    DateOnly Date,
    int OdometerStart,
    int OdometerEnd,
    int DistanceKm,
    decimal FuelLiters,
    decimal FuelCost,
    decimal KmPerLiter,
    decimal CostPerKm,
    string? Destination,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

public record CreateVehicleDailyUsageCommand(
    Guid VehicleId,
    DateOnly Date,
    int OdometerStart,
    int OdometerEnd,
    decimal FuelLiters,
    decimal FuelCost,
    string? Destination,
    string? Remarks) : ICommand<VehicleDailyUsageDto>;

public record UpdateVehicleDailyUsageCommand(
    Guid Id,
    DateOnly Date,
    int OdometerStart,
    int OdometerEnd,
    decimal FuelLiters,
    decimal FuelCost,
    string? Destination,
    string? Remarks) : ICommand<VehicleDailyUsageDto>;

public sealed class SearchVehicleDailyUsageQuery : IPagedQuery, IQuery<PagedResponse<VehicleDailyUsageDto>>
{
    public Guid? VehicleId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed class GetVehicleDailyUsageSummaryQuery : IQuery<VehicleDailyUsageSummaryDto>
{
    public Guid? VehicleId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public record VehicleDailyUsageSummaryDto(
    int RecordCount,
    int TotalDistanceKm,
    decimal TotalFuelLiters,
    decimal TotalFuelCost,
    decimal AverageKmPerLiter,
    decimal AverageCostPerKm,
    decimal AverageDistancePerDay);

public record MotorVehicleInventoryItemDto(
    int Qty,
    string Description,         // Make + Model
    string? MotorNumber,
    string? ChassisNumber,
    string? VehicleClassification, // e.g. PICK-UP VEHICLE, VAN TYPE VEHICLE
    string PlateNumber,
    string? VehicleUse,         // e.g. GOV'T-UV
    int? NumberOfCylinders,
    int? EngineDisplacementCC,
    string? FuelType,
    int Year,
    decimal? AcquisitionCost,
    string RunningCondition,    // derived from Status
    string? AccountableOfficer, // AssignedDriver
    string? AccountableOfficerTitle
);
