using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.DeleteLocation;

public sealed record DeleteLocationCommand(Guid Id) : ICommand;
