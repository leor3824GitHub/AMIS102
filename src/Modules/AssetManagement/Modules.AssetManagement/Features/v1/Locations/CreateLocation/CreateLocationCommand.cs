using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.CreateLocation;

public sealed record CreateLocationCommand(
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description) : ICommand<LocationDto>;
