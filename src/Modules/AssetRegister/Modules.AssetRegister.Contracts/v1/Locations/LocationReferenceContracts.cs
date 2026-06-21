using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Locations;

/// <summary>
/// Lightweight location reference for cross-module name resolution (e.g. printing a property
/// sticker). Intentionally minimal — does not expose the location type/hierarchy held by the
/// implementation's full <c>LocationDto</c>.
/// </summary>
public sealed record LocationReferenceDto(Guid Id, string Code, string Name);

public sealed record GetLocationReferenceByIdQuery(Guid Id) : IQuery<LocationReferenceDto?>;
