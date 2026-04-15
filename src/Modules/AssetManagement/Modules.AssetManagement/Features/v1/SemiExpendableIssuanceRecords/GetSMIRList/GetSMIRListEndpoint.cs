using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRList;

public static class GetSMIRListEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetSMIRList)
            .WithName(nameof(GetSMIRListQuery))
            .WithSummary("Get a paginated list of Semi-expendable Materials Issuance Records")
            .Produces<PagedSMIRListResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableIssuanceRecords.View);

    private static async Task<IResult> GetSMIRList(
        [AsParameters] GetSMIRListQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
