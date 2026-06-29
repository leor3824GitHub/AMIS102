using AMIS.Framework.Shared.Inspections;
using AMIS.Modules.AssetRegister.Contracts.v1.Inspections;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Inspections;

internal static class GetMyPendingReturnedPropertyInspectionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/pending-for-me", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetMyPendingReturnedPropertyInspectionsQuery(), ct)))
            .WithModuleName<GetMyPendingReturnedPropertyInspectionsQuery>()
            .WithSummary("List the returned-property requests awaiting the current user's inspection")
            .Produces<IReadOnlyList<PendingInspectionItem>>(StatusCodes.Status200OK)
            .RequireAuthorization();
}
