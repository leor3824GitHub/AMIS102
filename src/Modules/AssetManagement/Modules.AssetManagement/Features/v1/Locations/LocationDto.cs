using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.Locations;

public sealed record LocationDto(
    Guid Id,
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description);