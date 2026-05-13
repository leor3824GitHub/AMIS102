using AMIS.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.GetLocationById;

public sealed record GetLocationByIdQuery(Guid Id) : IQuery<LocationDto>;
