using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid Id,
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description) : ICommand<LocationDto>;