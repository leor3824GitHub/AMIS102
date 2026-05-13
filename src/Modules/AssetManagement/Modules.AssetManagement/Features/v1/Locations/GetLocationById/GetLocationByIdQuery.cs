using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.GetLocationById;

public sealed record GetLocationByIdQuery(Guid Id) : IQuery<LocationDto>;