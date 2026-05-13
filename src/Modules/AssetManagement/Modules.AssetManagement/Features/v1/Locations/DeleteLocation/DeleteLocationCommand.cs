using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.DeleteLocation;

public sealed record DeleteLocationCommand(Guid Id) : ICommand<Unit>;
