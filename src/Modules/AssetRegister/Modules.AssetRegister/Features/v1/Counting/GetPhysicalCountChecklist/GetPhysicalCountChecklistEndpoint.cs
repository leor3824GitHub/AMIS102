using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.GetPhysicalCountChecklist;

public static class GetPhysicalCountChecklistEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/checklist", Handle)
            .WithModuleName<GetPhysicalCountChecklistQuery>()
            .WithSummary("Coverage checklist for a count session: every in-scope asset tagged Counted/Missing/Uncounted, with location + accountable officer")
            .Produces<PhysicalCountChecklistDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Count.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPhysicalCountChecklistQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
