using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Vehicle.Contracts.v1.References;

public sealed record VehicleReferenceDto(
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
    string? AssignedDriver);

public sealed record GetVehicleReferenceByIdQuery(Guid Id) : IQuery<VehicleReferenceDto?>;

public sealed class SearchVehicleReferencesQuery : IPagedQuery, IQuery<PagedResponse<VehicleReferenceDto>>
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public Guid? AssignedDepartmentId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}
