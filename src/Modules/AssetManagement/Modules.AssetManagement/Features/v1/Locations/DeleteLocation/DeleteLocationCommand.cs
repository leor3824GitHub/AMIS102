using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.DeleteLocation;

public sealed record DeleteLocationCommand(Guid Id) : ICommand<Unit>;