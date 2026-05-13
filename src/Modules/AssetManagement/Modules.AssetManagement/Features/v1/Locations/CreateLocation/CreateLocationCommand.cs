using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.CreateLocation;

public sealed record CreateLocationCommand(
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description) : ICommand<LocationDto>;