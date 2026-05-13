using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.ReportSignatories.GetReportSignatories;

public static class GetReportSignatoriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetSignatories)
            .WithName(nameof(GetReportSignatoriesQuery))
            .WithSummary("Get report signatories by report type")
            .Produces<List<ReportSignatoryDto>>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.ReportSignatories.View);

    private static async Task<IResult> GetSignatories(
        [AsParameters] GetReportSignatoriesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

