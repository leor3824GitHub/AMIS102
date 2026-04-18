using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.DeleteTangibleItem;

public sealed record DeleteTangibleItemCommand(Guid Id) : ICommand<Unit>;
