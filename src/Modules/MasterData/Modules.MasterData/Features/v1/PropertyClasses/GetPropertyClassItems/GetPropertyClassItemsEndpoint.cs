using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassItems;

public static class GetPropertyClassItemsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/items", async (
            string? classCode,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPropertyClassItemsQuery(classCode), ct)))
        .WithName(nameof(GetPropertyClassItemsQuery))
        .WithSummary("Get property class items (category codes), optionally filtered by class code")
        .Produces<IReadOnlyList<PropertyClassItemDto>>(StatusCodes.Status200OK)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.View);
}
