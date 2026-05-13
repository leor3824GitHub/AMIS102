using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Counting.ReconcilePhysicalCount;

public static class ReconcilePhysicalCountEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/reconcile", Handle)
            .WithName(nameof(ReconcilePhysicalCountCommand))
            .WithSummary("Reconcile a count session — materializes FoundAtStation entries and opens incident drafts for missing")
            .Produces<PhysicalCountSessionDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Count.Submit);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ReconcilePhysicalCountCommand(id), ct);
        return TypedResults.Ok(result);
    }
}
