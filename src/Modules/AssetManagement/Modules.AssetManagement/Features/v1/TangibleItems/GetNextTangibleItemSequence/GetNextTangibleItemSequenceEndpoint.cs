using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetNextTangibleItemSequence;

public static class GetNextTangibleItemSequenceEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/next-sequence", GetNextSequence)
            .WithName(nameof(GetNextTangibleItemSequenceQuery))
            .WithSummary("Get next available sequence number for a property number")
            .Produces<NextTangibleItemSequenceResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleItems.View);

    private static async Task<IResult> GetNextSequence(
        int year,
        string officeCode,
        string classCode,
        string itemCode,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetNextTangibleItemSequenceQuery(year, officeCode, classCode, itemCode);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
