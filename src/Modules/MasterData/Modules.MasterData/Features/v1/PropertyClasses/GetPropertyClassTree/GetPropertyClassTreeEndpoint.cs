using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassTree;

public static class GetPropertyClassTreeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/tree", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPropertyClassTreeQuery(), ct)))
        .WithName(nameof(GetPropertyClassTreeQuery))
        .WithSummary("Get COA GAM Annex A property classification tree (all classes with items)")
        .Produces<IReadOnlyList<PropertyClassDto>>(StatusCodes.Status200OK)
        .RequirePermission(MasterDataModuleConstants.Permissions.PropertyClasses.View);
}

