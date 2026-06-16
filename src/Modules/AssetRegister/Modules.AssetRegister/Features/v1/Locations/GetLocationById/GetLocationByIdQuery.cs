using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocationById;

public sealed record GetLocationByIdQuery(Guid Id) : IQuery<LocationDto>;
