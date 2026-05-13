using AMIS.Modules.AssetManagement.Domain;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations;

public sealed record LocationDto(
    Guid Id,
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description);
