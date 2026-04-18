using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItemById;

public sealed record GetTangibleItemByIdQuery(Guid Id) : IQuery<TangibleItemDto>;
