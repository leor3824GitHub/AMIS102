using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.ReportSignatories.CreateReportSignatory;

public static class CreateReportSignatoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Create)
            .WithName(nameof(CreateReportSignatoryCommand))
            .WithSummary("Create a report signatory")
            .Produces<ReportSignatoryDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataModuleConstants.Permissions.ReportSignatories.Create);

    private static async Task<IResult> Create(
        CreateReportSignatoryCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/report-signatories/{result.Id}", result);
    }
}

