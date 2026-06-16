using AMIS.Modules.AssetRegister.Domain.Locations;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations;

public sealed record LocationDto(
    Guid Id,
    string Code,
    string Name,
    LocationType Type,
    Guid? ParentLocationId,
    string? Description);
