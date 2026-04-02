using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Vehicle.Contracts.v1.Vehicles;

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
    string? Notes,
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
    string? Notes = null) : ICommand<VehicleDto>;

public record UpdateVehicleCommand(
    Guid Id,
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    string Type,
    string? Notes) : ICommand<VehicleDto>;

public record AssignVehicleCommand(
    Guid Id,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DriverId,
    string? DriverName) : ICommand<Unit>;

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
