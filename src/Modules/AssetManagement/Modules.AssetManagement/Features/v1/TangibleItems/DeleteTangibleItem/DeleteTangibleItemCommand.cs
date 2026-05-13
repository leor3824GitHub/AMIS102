using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.DeleteTangibleItem;

public sealed record DeleteTangibleItemCommand(Guid Id) : ICommand<Unit>;

