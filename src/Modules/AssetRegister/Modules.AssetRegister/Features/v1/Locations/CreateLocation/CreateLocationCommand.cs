using AMIS.Modules.AssetRegister.Domain.Locations;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.CreateLocation;

public sealed record CreateLocationCommand(
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description) : ICommand<LocationDto>;
