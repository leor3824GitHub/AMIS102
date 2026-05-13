using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.DeleteTangibleInventory;

public sealed record DeleteTangibleInventoryCommand(Guid Id) : ICommand<Unit>;
